using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Classrooms;
using GrammarErrorDetection.Models.Grammar.Evaluations;
using GrammarErrorDetection.Models.Grammar.ModuleNames;
using System.Data;

namespace GrammarErrorDetection.Services.Services;

public sealed class RegistrationService : IRegistrationService
{
    private readonly IRegistrationRepository repository;
    private readonly RegistrationGenerationRunner generation;
    private readonly IClassRepository classes;
    private readonly IClassroomRepository classrooms;
    private readonly IModuleNameRepository modules;
    private readonly IEvaluationRepository evaluations;

    public RegistrationService(IRegistrationRepository repository, IClassRepository classes,
        IClassroomRepository classrooms, IModuleNameRepository modules, IEvaluationRepository evaluations)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(classes);
        ArgumentNullException.ThrowIfNull(classrooms);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(evaluations);
        this.repository = repository;
        this.classes = classes;
        this.classrooms = classrooms;
        this.modules = modules;
        this.evaluations = evaluations;
        generation = new RegistrationGenerationRunner(repository, classes, classrooms, modules, evaluations);
    }

    public Task<List<RegistrationListItemDto>> List(PaginationDto<RegistrationSummaryList> request)
        => repository.ListSummary(request);

    public Task<PagedList<RegistrationListItemDto>> ListPage(PaginationDto<RegistrationSummaryList> request)
        => repository.ListSummaryPage(request);

    public Task<RegistrationEntity> Insert(RegistrationInsert request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (IsStatus(request.Status, "Rejected")) request.Active = false;
        return repository.Insert(request);
    }

    public async Task<RegistrationEntity> Update(RegistrationUpdate request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var current = await FindRegistration(request.Id);
        if (IsStatus(request.Status ?? current.Status, "Rejected")) request.Active = false;
        return await repository.Update(request);
    }

    public Task<List<RegistrationListItemDto>> ListMine(Guid studentId, PaginationDto<RegistrationSummaryList> request)
    {
        ValidateStudentId(studentId);
        ArgumentNullException.ThrowIfNull(request);
        request.Filter ??= new RegistrationSummaryList();
        request.Filter.UserId = studentId;
        return repository.ListSummary(request);
    }

    public Task<PagedList<RegistrationListItemDto>> ListMinePage(Guid studentId, PaginationDto<RegistrationSummaryList> request)
    {
        ValidateStudentId(studentId);
        ArgumentNullException.ThrowIfNull(request);
        request.Filter ??= new RegistrationSummaryList();
        request.Filter.UserId = studentId;
        return repository.ListSummaryPage(request);
    }

    public async Task<List<AvailableRegistrationCourseDto>> ListAvailableCourses(Guid studentId,
        PaginationDto<AvailableRegistrationCourseList> request)
    {
        ValidateStudentId(studentId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageSize, 1);
        var filter = request.Filter ?? throw new ValidationException("Indicar año y bimestre.");
        ValidateApplication(filter);
        var snapshot = await LoadApplicationSnapshot();
        var offset = ((long)request.PageNumber - 1) * request.PageSize;
        if (offset > int.MaxValue) return [];
        return AvailableCourses(studentId, filter.Year, filter.Bimester, snapshot)
            .Skip((int)offset).Take(request.PageSize).ToList();
    }

    public async Task<RegistrationEntity> Submit(Guid studentId, StudentRegistrationRequest request)
    {
        ValidateStudentId(studentId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateApplication(request);
        var snapshot = await LoadApplicationSnapshot();
        var course = snapshot.Classes.SingleOrDefault(c => c.Id == request.ClassId)
            ?? throw new KeyNotFoundException("El curso no existe.");
        if (HasApproved(studentId, course.Year, course.Bimester, snapshot))
            throw new ValidationException("Ya tienes una inscripción aprobada en este año y bimestre.");
        if (!AvailableCourses(studentId, course.Year, course.Bimester, snapshot).Any(c => c.ClassId == course.Id))
            throw new ValidationException("El curso no está disponible o no cumples los requisitos del módulo.");

        var own = snapshot.Registrations.Where(r => r.UserId == studentId).ToList();
        var candidates = own.Where(r => InPeriod(r, course.Year, course.Bimester, snapshot)
            && !IsStatus(r.Status, "Approved")).ToList();
        var pending = candidates.Where(r => r.Active == true && IsStatus(r.Status, "Pending")).ToList();
        if (pending.Count > 1)
            throw new ValidationException("Tienes varias inscripciones pendientes en el periodo; el coordinador debe resolverlas.");
        var existing = pending.SingleOrDefault()
            ?? candidates.OrderByDescending(r => r.UpdatedAt).ThenByDescending(r => r.CreatedAt).ThenBy(r => r.Id).FirstOrDefault()
            ?? own.Where(r => r.ClassId is null && IsStatus(r.Status, "Pending"))
                .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id).FirstOrDefault();
        if (existing is not null)
            return await repository.Update(new RegistrationUpdate
            {
                Id = existing.Id, ClassId = course.Id, Status = "Pending", Active = true,
                RegistrationDate = DateTime.UtcNow, InvoiceLink = request.InvoiceLink ?? existing.InvoiceLink
            });
        return await repository.Insert(new RegistrationInsert
        {
            UserId = studentId, ClassId = course.Id, Status = "Pending", Active = true,
            RegistrationDate = DateTime.UtcNow, InvoiceLink = request.InvoiceLink
        });
    }

    public async Task<RegistrationEntity> Decide(Guid registrationId, RegistrationDecisionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateApplication(request);
        var current = await FindRegistration(registrationId);
        if (!current.Active.HasValue)
            throw new ValidationException("Registration_List no devuelve Active; se necesita para resolver una inscripción vigente.");
        if (current.Active != true || !IsStatus(current.Status, "Pending"))
            throw new ValidationException("Solo se pueden resolver inscripciones activas pendientes.");
        if (request.Approved == true)
        {
            var snapshot = await LoadApplicationSnapshot();
            if (current.ClassId is not Guid classId
                || !snapshot.Classes.Any(c => c.Id == classId
                    && AvailableCourses(current.UserId, c.Year, c.Bimester, snapshot).Any(a => a.ClassId == classId)))
                throw new ValidationException("El curso ya no está disponible o el estudiante no cumple sus requisitos.");
        }
        // Active=false es la fuente de pertenencia al curso utilizada por User_List y Class_List.
        return await repository.Update(new RegistrationUpdate
        {
            Id = registrationId, Status = request.Approved == true ? "Approved" : "Rejected",
            Active = request.Approved == true
        });
    }

    private sealed record ApplicationSnapshot(List<ClassEntity> Classes, List<ModuleNameEntity> Modules,
        List<ClassroomEntity> Rooms, List<RegistrationEntity> Registrations, List<EvaluationEntity> Evaluations);

    private async Task<ApplicationSnapshot> LoadApplicationSnapshot()
    {
        var snapshot = new ApplicationSnapshot(
        await ReadApplications(classes.List, new ClassList()),
        await ReadApplications(modules.List, new ModuleNameList()),
        await ReadApplications(classrooms.List, new ClassroomList { Active = true }),
        await ReadApplications(repository.List, new RegistrationList()),
        await ReadApplications(evaluations.List, new EvaluationList { Active = true }));
        if (snapshot.Registrations.Any(r => !r.Active.HasValue))
            throw new ValidationException("Registration_List no devuelve Active; se necesita para validar inscripciones vigentes y cupos.");
        return snapshot;
    }

    private static List<AvailableRegistrationCourseDto> AvailableCourses(Guid studentId, int year, int bimester,
        ApplicationSnapshot snapshot)
    {
        if (HasApproved(studentId, year, bimester, snapshot)) return [];
        var classMap = snapshot.Classes.ToDictionary(c => c.Id);
        var moduleMap = snapshot.Modules.ToDictionary(m => m.Id);
        var rooms = snapshot.Rooms.Where(r => r.Active == true).ToDictionary(r => r.Id);
        var history = snapshot.Registrations.Where(r => r.UserId == studentId && r.Active == true
            && ManualClassEligibility.ReservesSeat(r) && r.ClassId.HasValue && classMap.ContainsKey(r.ClassId.Value))
            .Where(r => classMap[r.ClassId!.Value].Year < year
                || (classMap[r.ClassId!.Value].Year == year && classMap[r.ClassId.Value].Bimester < bimester))
            .OrderByDescending(r => classMap[r.ClassId!.Value].Year)
            .ThenByDescending(r => classMap[r.ClassId!.Value].Bimester)
            .ThenByDescending(r => r.RegistrationDate).ThenByDescending(r => r.CreatedAt).ThenBy(r => r.Id).ToList();
        HashSet<Guid> allowed;
        if (history.FirstOrDefault() is { } previous)
        {
            var score = RegistrationEvaluationCalculator.Calculate(snapshot.Evaluations.Where(e => e.RegistrationId == previous.Id)).FinalScore;
            if (!score.HasValue) return [];
            var previousModuleId = classMap[previous.ClassId!.Value].ModuleNameId;
            var targetId = score.Value < 70m ? previousModuleId : moduleMap.GetValueOrDefault(previousModuleId)?.NextModule;
            allowed = targetId.HasValue ? [targetId.Value] : [];
        }
        else
        {
            var incoming = snapshot.Modules.Where(m => m.NextModule.HasValue).Select(m => m.NextModule!.Value).ToHashSet();
            allowed = snapshot.Modules.Where(m => m.Active && !incoming.Contains(m.Id)).Select(m => m.Id).ToHashSet();
        }

        var today = DateTime.UtcNow.AddHours(-4).Date;
        var occupied = snapshot.Registrations.Where(r => r.Active == true && ManualClassEligibility.ReservesSeat(r)
            && r.ClassId.HasValue && r.UserId != studentId).ToLookup(r => r.ClassId!.Value);
        return snapshot.Classes.Where(c => c.Active && c.Year == year && c.Bimester == bimester
                && allowed.Contains(c.ModuleNameId) && moduleMap.TryGetValue(c.ModuleNameId, out var module) && module.Active
                && rooms.ContainsKey(c.ClassroomId) && (c.EndDate is null || c.EndDate.Value.Date >= today)
                && c.StartTime < c.EndTime && occupied[c.Id].Count() < rooms[c.ClassroomId].Capacity)
            .OrderBy(c => moduleMap[c.ModuleNameId].Name).ThenBy(c => c.Parallel).ThenBy(c => c.StartTime).ThenBy(c => c.Id)
            .Select(c => new AvailableRegistrationCourseDto
            {
                ClassId = c.Id, ModuleNameId = c.ModuleNameId, ModuleName = moduleMap[c.ModuleNameId].Name,
                ClassroomId = c.ClassroomId, ClassroomName = rooms[c.ClassroomId].Name,
                Parallel = c.Parallel, StartTime = c.StartTime, EndTime = c.EndTime,
                StartDate = c.StartDate, EndDate = c.EndDate, Year = c.Year, Bimester = c.Bimester,
                AvailableSeats = rooms[c.ClassroomId].Capacity - occupied[c.Id].Count()
            }).ToList();
    }

    private static bool InPeriod(RegistrationEntity registration, int year, int bimester, ApplicationSnapshot snapshot) =>
        snapshot.Classes.Any(c => c.Id == registration.ClassId && c.Year == year && c.Bimester == bimester);

    private static bool HasApproved(Guid studentId, int year, int bimester, ApplicationSnapshot snapshot) =>
        snapshot.Registrations.Any(r => r.UserId == studentId && r.Active == true && IsStatus(r.Status, "Approved")
            && InPeriod(r, year, bimester, snapshot));

    private async Task<RegistrationEntity> FindRegistration(Guid id)
    {
        if (id == Guid.Empty) throw new ValidationException("El identificador de inscripción no puede estar vacío.");
        return (await ReadApplications(repository.List, new RegistrationList { Id = id })).SingleOrDefault(r => r.Id == id)
            ?? throw new KeyNotFoundException("La inscripción no existe.");
    }

    private static bool IsStatus(string? status, string expected) => string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);

    private static void ValidateStudentId(Guid id)
    {
        if (id == Guid.Empty) throw new ValidationException("El identificador de estudiante no puede estar vacío.");
    }

    private static void ValidateApplication(object request) =>
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);

    private static async Task<List<TEntity>> ReadApplications<TFilter, TEntity>(
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

    public async Task<RegistrationGenerationResult> GenerateNextBimester(RegistrationGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        var input = await generation.Load(request);
        var sourceClassIds = input.SourceClasses.Select(c => c.Id).ToHashSet();
        var evaluationGroups = input.Evaluations.ToLookup(e => e.RegistrationId);
        var passed = new HashSet<Guid>();
        var failed = new HashSet<Guid>();
        var missingGrades = new Dictionary<Guid, string>();
        foreach (var registration in input.Registrations.Where(r =>
                     r.ClassId.HasValue && sourceClassIds.Contains(r.ClassId.Value)))
        {
            var grade = RegistrationEvaluationCalculator.Calculate(evaluationGroups[registration.Id]);
            if (grade.FinalScore is null)
                missingGrades[registration.Id] = grade.Reason!;
            else if (grade.FinalScore >= 70m)
                passed.Add(registration.Id);
            else
                failed.Add(registration.Id);
        }

        var result = await generation.Save(request, input, passed, failed);
        foreach (var skipped in result.Skipped)
            if (missingGrades.TryGetValue(skipped.RegistrationId, out var reason))
                skipped.Reason = reason;
        return result;
    }

    public async Task<List<StudentCourseHistoryDto>> ListStudentCourses(Guid studentId, StudentCourseHistoryQuery request)
        => (await ListStudentCoursesPage(studentId, request)).Items;

    // El total es la cantidad de cursos del estudiante (sin rechazados ni cancelados) antes de aplicar la página.
    public async Task<PagedList<StudentCourseHistoryDto>> ListStudentCoursesPage(Guid studentId, StudentCourseHistoryQuery request)
    {
        ValidateStudentId(studentId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateApplication(request);

        // El SP ordena por CreatedAt; se recorre el listado para ordenar por la
        // fecha real de inscripción antes de aplicar la página solicitada.
        var registrations = await ReadApplications(repository.ListSummary, new RegistrationSummaryList { UserId = studentId });
        var offset = ((long)request.PageNumber - 1) * request.PageSize;
        var eligible = registrations.Where(r => r.UserId == studentId && r.ClassId.HasValue
                && !IsStatus(r.Status.Trim(), "Rejected") && !IsStatus(r.Status.Trim(), "Cancelled")
                && !IsStatus(r.Status.Trim(), "Canceled"))
            .OrderByDescending(r => r.RegistrationDate).ThenByDescending(r => r.UpdatedAt)
            .ThenByDescending(r => r.RegistrationId).ToList();
        if (offset > int.MaxValue) return new PagedList<StudentCourseHistoryDto>([], eligible.Count);
        var selected = eligible.Skip((int)offset).Take(request.PageSize).ToList();

        var result = new List<StudentCourseHistoryDto>();
        var courseCache = new Dictionary<Guid, ClassEntity>();
        foreach (var registration in selected)
        {
            var classId = registration.ClassId!.Value;
            if (!courseCache.TryGetValue(classId, out var course))
            {
                // Incluye cursos anteriores aunque ya estén inactivos.
                course = (await ReadApplications(classes.List, new ClassList { Id = classId }))
                    .SingleOrDefault(c => c.Id == classId)
                    ?? throw new DataException($"Class_List no devolvió el curso {classId} de la inscripción {registration.RegistrationId}.");
                courseCache.Add(classId, course);
            }
            var grades = await ReadApplications(evaluations.List,
                new EvaluationList { RegistrationId = registration.RegistrationId, Active = true });
            var ownGrades = grades.Where(e => e.RegistrationId == registration.RegistrationId).ToList();
            result.Add(new StudentCourseHistoryDto
            {
                RegistrationId = registration.RegistrationId, ClassId = classId,
                ModuleId = course.ModuleNameId, ModuleName = course.ModuleName ?? registration.ModuleName,
                RegistrationDate = registration.RegistrationDate, TeacherName = course.TeacherName,
                Exam1Score = RegistrationEvaluationCalculator.CalculateExam(ownGrades, 1),
                Exam2Score = RegistrationEvaluationCalculator.CalculateExam(ownGrades, 2),
                Parallel = course.Parallel, StartTime = course.StartTime, EndTime = course.EndTime,
                Year = course.Year, Bimester = course.Bimester, Status = registration.Status
            });
        }
        return new PagedList<StudentCourseHistoryDto>(result, eligible.Count);
    }
}

public sealed class EvaluationService : IEvaluationService
{
    private readonly IEvaluationRepository repository;

    public EvaluationService(IEvaluationRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        this.repository = repository;
    }

    public Task<List<EvaluationEntity>> List(PaginationDto<EvaluationList> request) => repository.List(request);

    public Task<PagedList<EvaluationEntity>> ListPage(PaginationDto<EvaluationList> request) => repository.ListPage(request);
    public Task<EvaluationEntity> Insert(EvaluationInsert request) => repository.Insert(request);
    public Task<EvaluationEntity> Update(EvaluationUpdate request) => repository.Update(request);
}

internal static class RegistrationEvaluationCalculator
{
    internal sealed record Result(decimal? FinalScore, string? Reason);

    internal static Result Calculate(IEnumerable<EvaluationEntity> evaluations)
    {
        var active = evaluations.Where(e => e.Active).ToList();
        if (active.Count != 2 || active.Count(e => e.Period == 1) != 1 || active.Count(e => e.Period == 2) != 1)
            return new(null, "Se requiere exactamente una evaluación activa del periodo 1 y una del periodo 2.");

        var totals = new List<decimal>();
        foreach (var evaluation in active)
        {
            var result = CalculateEvaluation(evaluation);
            if (result.FinalScore is not decimal score) return result;
            totals.Add(score);
        }

        // Los componentes ya son puntos ponderados. No se aplican porcentajes otra vez.
        return new((totals[0] + totals[1]) / 2m, null);
    }

    internal static decimal? CalculateExam(IEnumerable<EvaluationEntity> evaluations, int period)
    {
        var matching = evaluations.Where(e => e.Active && e.Period == period).ToList();
        return matching.Count == 1 ? CalculateEvaluation(matching[0]).FinalScore : null;
    }

    private static Result CalculateEvaluation(EvaluationEntity evaluation)
    {
        if (evaluation.WritingScore is null || evaluation.OralScore is null
            || evaluation.ListeningScore is null || evaluation.ReadingScore is null
            || evaluation.GrammarScore is null || evaluation.AcademicPerformance is null)
            return new(null, $"La evaluación del periodo {evaluation.Period} tiene componentes sin calificar.");

        // El formulario captura "Gramática y vocabulario" como un solo puntaje en GrammarScore (máximo 10 compartido):
        // un VocabularyScore vacío cuenta como 0 y solo se suma cuando existe (datos que traen ambos).
        var vocabulary = evaluation.VocabularyScore ?? 0;
        if (!Within(evaluation.WritingScore.Value, 30) || !Within(evaluation.OralScore.Value, 15)
            || !Within(evaluation.ListeningScore.Value, 10) || !Within(evaluation.ReadingScore.Value, 10)
            || !Within(evaluation.GrammarScore.Value, 10) || !Within(vocabulary, 10)
            || evaluation.GrammarScore.Value + vocabulary > 10
            || !Within(evaluation.AcademicPerformance.Value, 25))
            return new(null, $"La evaluación del periodo {evaluation.Period} tiene notas fuera de rango.");

        return new(evaluation.WritingScore!.Value + evaluation.OralScore!.Value
            + evaluation.ListeningScore!.Value + evaluation.ReadingScore!.Value
            + evaluation.GrammarScore!.Value + vocabulary
            + evaluation.AcademicPerformance!.Value, null);
    }

    private static bool Within(int score, int maximum) => score >= 0 && score <= maximum;
}
