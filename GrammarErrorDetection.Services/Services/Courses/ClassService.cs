using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Classrooms;
using GrammarErrorDetection.Models.Grammar.Evaluations;
using GrammarErrorDetection.Models.Grammar.ModuleNames;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Models.Grammar.Roles;
using GrammarErrorDetection.Models.Grammar.Users;

namespace GrammarErrorDetection.Services.Services;

public sealed class ClassService : IClassService
{
    private readonly IClassRepository repository;
    private readonly IClassroomRepository classrooms;
    private readonly IRegistrationRepository registrations;
    private readonly IModuleNameRepository modules;
    private readonly IEvaluationRepository evaluations;
    private readonly IUserRepository users;
    private readonly IRoleRepository roles;

    public ClassService(IClassRepository repository, IClassroomRepository classrooms,
        IRegistrationRepository registrations, IModuleNameRepository modules,
        IEvaluationRepository evaluations, IUserRepository users, IRoleRepository roles)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(classrooms);
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(evaluations);
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(roles);
        this.repository = repository;
        this.classrooms = classrooms;
        this.registrations = registrations;
        this.modules = modules;
        this.evaluations = evaluations;
        this.users = users;
        this.roles = roles;
    }

    public Task<List<ClassEntity>> List(PaginationDto<ClassList> request)
        => repository.List(request);

    public Task<PagedList<ClassEntity>> ListPage(PaginationDto<ClassList> request) => repository.ListPage(request);

    public Task<ClassEntity> Insert(ClassInsert request)
        => repository.Insert(request);

    public Task<ClassEntity> Update(ClassUpdate request)
        => repository.Update(request);

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
            var course = snapshot.Classes.SingleOrDefault(c => c.Id == classId) ?? throw new ValidationException("El curso no existe.");
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

internal static class ManualClassEligibility
{
    internal sealed record Candidate(EligibleStudentDto Dto,
        RegistrationEntity? StatusSource, RegistrationEntity? RegistrationToReplace);

    internal static List<Candidate> Build(EligibleStudentList filter, IReadOnlyCollection<UserEntity> students,
        IReadOnlyCollection<ClassEntity> courses, IReadOnlyCollection<ModuleNameEntity> modules,
        IReadOnlyCollection<RegistrationEntity> registrations, IReadOnlyCollection<EvaluationEntity> evaluations)
    {
        var classes = courses.ToDictionary(c => c.Id);
        var moduleMap = modules.ToDictionary(m => m.Id);
        var incoming = modules.Where(m => m.NextModule.HasValue).Select(m => m.NextModule!.Value).ToHashSet();
        var grades = evaluations.ToLookup(e => e.RegistrationId);
        var byStudent = registrations.Where(r => r.Active == true).ToLookup(r => r.UserId);
        var results = new List<Candidate>();
        foreach (var student in students.Where(s => s.Active).OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ThenBy(s => s.Id))
        {
            if (!Matches(student.FirstName, filter.FirstName) || !Matches(student.LastName, filter.LastName)
                || !Matches(student.CI, filter.CI)) continue;
            var history = byStudent[student.Id].Where(r => ReservesSeat(r)
                && r.ClassId.HasValue && classes.ContainsKey(r.ClassId.Value)).ToList();
            var assigned = history.Where(r => IsTarget(classes[r.ClassId!.Value], filter))
                .OrderByDescending(r => r.RegistrationDate).ThenByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).ToList();
            var previous = history.Where(r => IsBefore(classes[r.ClassId!.Value], filter))
                .OrderByDescending(r => classes[r.ClassId!.Value].Year)
                .ThenByDescending(r => classes[r.ClassId!.Value].Bimester)
                .ThenByDescending(r => r.RegistrationDate).ThenByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).FirstOrDefault();
            var sameModule = assigned.FirstOrDefault(r => classes[r.ClassId!.Value].ModuleNameId == filter.ModuleNameId);
            string? reason = sameModule is not null ? "AlreadyAssigned" : null;
            decimal? score = null;
            if (reason is null && previous is not null)
            {
                var priorModuleId = classes[previous.ClassId!.Value].ModuleNameId;
                score = RegistrationEvaluationCalculator.Calculate(grades[previous.Id]).FinalScore;
                if (score >= 70m && moduleMap.TryGetValue(priorModuleId, out var priorModule)
                    && priorModule.NextModule == filter.ModuleNameId) reason = "PassedPreviousModule";
                else if (score < 70m && priorModuleId == filter.ModuleNameId) reason = "RepeatModule";
            }
            // Un módulo de entrada no tiene otro módulo que lo señale como NextModule.
            if (reason is null && previous is null && assigned.Count == 0
                && history.Count == 0 && !incoming.Contains(filter.ModuleNameId)) reason = "NewStudent";
            if (reason is null || (filter.IsAssigned.HasValue && filter.IsAssigned != (assigned.Count > 0))) continue;

            var unassigned = byStudent[student.Id].Where(r => r.ClassId is null)
                .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).FirstOrDefault();
            // Un solo destino previo permite un traslado inequívoco. Si hay varios,
            // se muestran todos, pero el servicio exige resolver la ambigüedad.
            var replacement = assigned.Count == 1 ? assigned[0] : assigned.Count == 0 ? unassigned : null;
            var source = replacement ?? sameModule ?? previous;
            results.Add(new Candidate(new EligibleStudentDto
            {
                UserId = student.Id, FirstName = student.FirstName, LastName = student.LastName,
                CI = student.CI, Email = student.Email, Phone = student.Phone,
                EligibilityReason = reason, LastFinalScore = score, IsAssigned = assigned.Count > 0,
                RegistrationStatus = source?.Status ?? "Pending",
                Assignments = assigned.Select(r =>
                {
                    var course = classes[r.ClassId!.Value];
                    return new StudentAssignmentDto
                    {
                        RegistrationId = r.Id, ClassId = course.Id, ModuleNameId = course.ModuleNameId,
                        ModuleName = moduleMap.GetValueOrDefault(course.ModuleNameId)?.Name ?? string.Empty,
                        Parallel = course.Parallel, StartTime = course.StartTime, EndTime = course.EndTime,
                        RegistrationStatus = r.Status
                    };
                }).ToList()
            }, source, replacement));
        }
        // Prioriza a quienes ya están en el módulo y periodo solicitados antes
        // de paginar. OrderByDescending conserva el orden alfabético de cada grupo.
        return results.OrderByDescending(c => c.Dto.EligibilityReason == "AlreadyAssigned").ToList();
    }

    internal static bool ReservesSeat(RegistrationEntity r) =>
        !string.Equals(r.Status, "Rejected", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
    private static bool Matches(string text, string? query) => string.IsNullOrWhiteSpace(query)
        || text.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
    private static bool IsTarget(ClassEntity c, EligibleStudentList f) => c.Year == f.Year && c.Bimester == f.Bimester;
    private static bool IsBefore(ClassEntity c, EligibleStudentList f) => c.Year < f.Year || (c.Year == f.Year && c.Bimester < f.Bimester);
}

public sealed class ClassroomService : IClassroomService
{
    private readonly IClassroomRepository repository;

    public ClassroomService(IClassroomRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        this.repository = repository;
    }

    public Task<List<ClassroomEntity>> List(PaginationDto<ClassroomList> request)
        => repository.List(request);

    public Task<PagedList<ClassroomEntity>> ListPage(PaginationDto<ClassroomList> request)
        => repository.ListPage(request);

    public Task<ClassroomEntity> Insert(ClassroomInsert request)
        => repository.Insert(request);

    public Task<ClassroomEntity> Update(ClassroomUpdate request)
        => repository.Update(request);
}

public sealed class ModuleNameService : IModuleNameService
{
    private readonly IModuleNameRepository repository;

    public ModuleNameService(IModuleNameRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        this.repository = repository;
    }

    public Task<List<ModuleNameEntity>> List(PaginationDto<ModuleNameList> request)
        => repository.List(request);

    public Task<PagedList<ModuleNameEntity>> ListPage(PaginationDto<ModuleNameList> request)
        => repository.ListPage(request);

    public Task<ModuleNameEntity> Insert(ModuleNameInsert request)
        => repository.Insert(request);

    public Task<ModuleNameEntity> Update(ModuleNameUpdate request)
        => repository.Update(request);
}
