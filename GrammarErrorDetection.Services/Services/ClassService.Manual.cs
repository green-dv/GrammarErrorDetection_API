using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Classrooms;
using GrammarErrorDetection.Models.Grammar.Evaluations;
using GrammarErrorDetection.Models.Grammar.ModuleNames;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Models.Grammar.Roles;
using GrammarErrorDetection.Models.Grammar.Users;

namespace GrammarErrorDetection.Services.Services;

public sealed partial class ClassService
{
    public async Task<List<EligibleStudentDto>> ListEligibleStudents(PaginationDto<EligibleStudentList> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageSize, 1);
        var filter = request.Filter ?? throw new ValidationException("Indicar módulo, año y bimestre.");
        Validate(filter);
        var snapshot = await LoadManualSnapshot(filter.ModuleNameId);
        if (filter.ClassId is Guid classId)
        {
            var course = snapshot.Classes.SingleOrDefault(c => c.Id == classId)
                ?? throw new ValidationException("El curso no existe.");
            if (course.ModuleNameId != filter.ModuleNameId || course.Year != filter.Year || course.Bimester != filter.Bimester)
                throw new ValidationException("El curso no corresponde al módulo, año y bimestre indicados.");
            var enrolled = snapshot.Registrations.Where(r => r.Active == true && r.ClassId == classId
                && ManualClassEligibility.ReservesSeat(r)).ToList();
            var grades = snapshot.Evaluations.ToLookup(e => e.RegistrationId);
            // Una clase vacía no está terminada. Cero y reprobación también son notas completas.
            if (enrolled.Count > 0 && enrolled.All(r =>
                RegistrationEvaluationCalculator.Calculate(grades[r.Id]).FinalScore.HasValue))
                return [];
        }
        var offset = ((long)request.PageNumber - 1) * request.PageSize;
        if (offset > int.MaxValue) return [];
        return Candidates(filter, snapshot).Skip((int)offset)
            .Take(request.PageSize).Select(c => c.Dto).ToList();
    }

    public async Task<ClassWithStudentsResult> InsertWithStudents(ClassWithStudentsInsert request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);
        var snapshot = await LoadManualSnapshot(request.ModuleNameId);
        await ValidateManualResources(request, request.StudentIds.Count, snapshot);

        var filter = new EligibleStudentList { ModuleNameId = request.ModuleNameId, Year = request.Year, Bimester = request.Bimester };
        var eligible = Candidates(filter, snapshot).ToDictionary(c => c.Dto.UserId);
        var selected = new List<ManualClassEligibility.Candidate>();
        foreach (var studentId in request.StudentIds)
            selected.Add(SelectCandidate(studentId, eligible));

        // Se valida toda la selección antes de la primera escritura. El guardado
        // usa los CRUD existentes, sin una transacción global, según el alcance acordado.
        var course = await repository.Insert(request);
        var result = new ClassWithStudentsResult { Class = course };
        foreach (var candidate in selected)
            result.Students.Add(await AssignStudent(course.Id, candidate));
        return result;
    }

    public async Task<ClassWithStudentsResult> UpdateWithStudents(ClassWithStudentsUpdate request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);
        var snapshot = await LoadManualSnapshot(request.ModuleNameId);
        var current = snapshot.Classes.SingleOrDefault(c => c.Id == request.Id)
            ?? throw new ValidationException("El curso no existe.");
        var currentRegistrations = snapshot.Registrations.Where(r => r.ClassId == current.Id).ToList();
        if (currentRegistrations.Count > 0 && (current.ModuleNameId != request.ModuleNameId
            || current.Year != request.Year || current.Bimester != request.Bimester))
            throw new ValidationException("No se puede cambiar el módulo o periodo de un curso con inscripciones activas. Crear un nuevo curso para trasladar estudiantes.");

        await ValidateManualResources(request, request.StudentIds.Count, snapshot, current.Id);
        var desired = request.StudentIds.ToHashSet();
        var retained = currentRegistrations.Where(r => desired.Contains(r.UserId)).ToList();
        if (retained.GroupBy(r => r.UserId).Any(g => g.Count() > 1))
            throw new ValidationException("El curso tiene inscripciones activas duplicadas para un estudiante; resolverlas antes de actualizarlo.");
        var retainedIds = retained.Select(r => r.UserId).ToHashSet();
        var filter = new EligibleStudentList { ModuleNameId = request.ModuleNameId, Year = request.Year, Bimester = request.Bimester };
        var eligible = Candidates(filter, snapshot).ToDictionary(c => c.Dto.UserId);
        var additions = request.StudentIds.Where(id => !retainedIds.Contains(id))
            .Select(id => SelectCandidate(id, eligible)).ToList();

        // Todas las validaciones se completan antes de modificar el curso o las inscripciones.
        var course = await repository.Update(new ClassUpdate
        {
            Id = request.Id, TeacherId = request.TeacherId, ModuleNameId = request.ModuleNameId,
            Year = request.Year, Bimester = request.Bimester, StartDate = request.StartDate,
            EndDate = request.EndDate, Parallel = request.Parallel, ClassroomId = request.ClassroomId,
            StartTime = request.StartTime, EndTime = request.EndTime, Active = request.Active
        });
        var result = new ClassWithStudentsResult { Class = course };
        var assignments = retained.ToDictionary(r => r.UserId, r => new StudentClassAssignmentResult
        {
            UserId = r.UserId, RegistrationId = r.Id, Status = r.Status
        });
        foreach (var candidate in additions)
            assignments.Add(candidate.Dto.UserId, await AssignStudent(course.Id, candidate));
        foreach (var registration in currentRegistrations.Where(r => !desired.Contains(r.UserId)))
        {
            await registrations.Update(new RegistrationUpdate { Id = registration.Id, Active = false });
            result.RemovedRegistrationIds.Add(registration.Id);
        }
        result.Students = request.StudentIds.Select(id => assignments[id]).ToList();
        return result;
    }

    private async Task ValidateManualResources(ClassInsert request, int studentCount, ManualSnapshot snapshot, Guid? excludedClassId = null)
    {
        var room = (await ReadAll(classrooms.List, new ClassroomList { Id = request.ClassroomId, Active = true }))
            .SingleOrDefault(c => c.Id == request.ClassroomId && c.Active)
            ?? throw new ValidationException("El aula no existe o está inactiva.");
        if (room.Capacity < studentCount)
            throw new ValidationException("La cantidad de estudiantes supera la capacidad del aula.");
        var teacherRoles = snapshot.Roles.Where(r => r.Name == "Teacher").Select(r => r.Id).ToHashSet();
        if (!snapshot.Users.Any(u => u.Id == request.TeacherId && u.Active && teacherRoles.Contains(u.RoleId)))
            throw new ValidationException("El profesor no existe, está inactivo o no tiene el rol Teacher.");
        if (snapshot.Classes.Any(c => c.Id != excludedClassId && c.Active && c.Year == request.Year && c.Bimester == request.Bimester
            && (c.ClassroomId == request.ClassroomId || c.TeacherId == request.TeacherId)
            && (c.StartDate is null || c.StartDate <= request.EndDate)
            && (c.EndDate is null || c.EndDate >= request.StartDate)
            && c.StartTime < request.EndTime && request.StartTime < c.EndTime))
            throw new ValidationException("El aula o el profesor ya tienen una clase que se superpone con esas fechas y horario.");
    }

    private static ManualClassEligibility.Candidate SelectCandidate(Guid studentId,
        Dictionary<Guid, ManualClassEligibility.Candidate> eligible)
    {
        if (!eligible.TryGetValue(studentId, out var candidate))
            throw new ValidationException($"El estudiante {studentId} no es elegible para el módulo y periodo seleccionados.");
        if (candidate.Dto.Assignments.Count > 1)
            throw new ValidationException($"El estudiante {studentId} tiene varias inscripciones activas en el periodo; resolverlas antes de trasladarlo.");
        return candidate;
    }

    private async Task<StudentClassAssignmentResult> AssignStudent(Guid classId, ManualClassEligibility.Candidate candidate)
    {
        var previous = candidate.RegistrationToReplace;
        var status = candidate.StatusSource?.Status ?? "Pending";
        // Primero se crea la nueva para no desactivar la anterior si el Insert falla.
        var registration = await registrations.Insert(new RegistrationInsert
        {
            UserId = candidate.Dto.UserId, ClassId = classId, Status = status,
            RegistrationDate = previous?.RegistrationDate ?? DateTime.UtcNow,
            InvoiceLink = previous?.InvoiceLink, Active = true
        });
        if (previous is not null)
            await registrations.Update(new RegistrationUpdate { Id = previous.Id, Active = false });
        return new StudentClassAssignmentResult
        {
            UserId = candidate.Dto.UserId, RegistrationId = registration.Id,
            DeactivatedRegistrationId = previous?.Id, Status = registration.Status
        };
    }

    private sealed record ManualSnapshot(List<ClassEntity> Classes, List<ModuleNameEntity> Modules,
        List<RegistrationEntity> Registrations, List<EvaluationEntity> Evaluations,
        List<UserEntity> Users, List<RoleEntity> Roles);

    private async Task<ManualSnapshot> LoadManualSnapshot(Guid moduleId)
    {
        var moduleList = await ReadAll(modules.List, new ModuleNameList());
        if (!moduleList.Any(m => m.Id == moduleId && m.Active))
            throw new ValidationException("El módulo no existe o está inactivo.");
        return new ManualSnapshot(
            await ReadAll(repository.List, new ClassList()), moduleList,
            await ReadAll(registrations.List, new RegistrationList { Active = true }),
            await ReadAll(evaluations.List, new EvaluationList { Active = true }),
            await ReadAll(users.List, new UserList { Active = true }),
            await ReadAll(roles.List, new RoleList { Active = true }));
    }

    private static List<ManualClassEligibility.Candidate> Candidates(EligibleStudentList filter, ManualSnapshot snapshot)
    {
        var studentRoles = snapshot.Roles.Where(r => r.Name == "Student").Select(r => r.Id).ToHashSet();
        return ManualClassEligibility.Build(filter, snapshot.Users.Where(u => studentRoles.Contains(u.RoleId)).ToList(),
            snapshot.Classes, snapshot.Modules, snapshot.Registrations, snapshot.Evaluations);
    }

    private static void Validate(object request) =>
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);

    private static async Task<List<TEntity>> ReadAll<TFilter, TEntity>(
        Func<PaginationDto<TFilter>, Task<List<TEntity>>> list, TFilter filter)
    {
        var result = new List<TEntity>();
        const int pageSize = 200;
        for (var page = 1; ; page++)
        {
            var batch = await list(new PaginationDto<TFilter> { PageNumber = page, PageSize = pageSize, Filter = filter });
            result.AddRange(batch);
            if (batch.Count < pageSize) return result;
        }
    }
}
