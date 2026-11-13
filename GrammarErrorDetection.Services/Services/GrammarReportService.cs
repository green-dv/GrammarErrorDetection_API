using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Models.Grammar.Reports;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class GrammarReportService(IGrammarReportRepository reports, IClassRepository classes,
    IRegistrationRepository registrations, GrammarReportExcelService excel) : IGrammarReportService
{
    public async Task<GrammarReportSummaryDto> Summary(ClaimsPrincipal user, GrammarReportQuery request)
        => await reports.Summary((await Prepare(user, request)).Scope);

    public async Task<List<GrammarReportCategoryDto>> Categories(ClaimsPrincipal user, GrammarReportQuery request)
        => await reports.Categories((await Prepare(user, request)).Scope);

    public async Task<List<GrammarReportComparisonDto>> Comparison(ClaimsPrincipal user, GrammarReportQuery request)
        => await reports.Comparison((await Prepare(user, request)).Scope);

    public async Task<GrammarReportFile> Export(ClaimsPrincipal user, GrammarReportQuery request)
    {
        var (scope, course, studentName) = await Prepare(user, request);
        var summary = await reports.Summary(scope);
        var categories = await reports.Categories(scope);
        var comparison = await reports.Comparison(scope);
        var previousCourses = await FindPreviousCourses(comparison);
        IReadOnlyList<StudentGrammarReport> students = course is null ? []
            : scope.UserId.HasValue ? [new(studentName, summary, categories)]
            : await ReadStudentReports(course);

        return excel.Create(new(summary, categories, comparison), course, studentName, previousCourses, students);
    }

    private async Task<List<StudentGrammarReport>> ReadStudentReports(ClassEntity course)
    {
        const int pageSize = 100;
        var students = new Dictionary<Guid, string>();
        for (var page = 1; ; page++)
        {
            var rows = await registrations.ListSummary(new PaginationDto<RegistrationSummaryList>
            {
                PageNumber = page,
                PageSize = pageSize,
                Filter = new() { ModuleNameId = course.ModuleNameId, Status = "Approved" }
            });
            foreach (var row in rows)
            {
                if (row.ClassId != course.Id || !string.Equals(row.Status.Trim(), "Approved", StringComparison.OrdinalIgnoreCase))
                    continue;

                var name = $"{row.FirstName} {row.LastName}".Trim();
                students.TryAdd(row.UserId, name.Length == 0 ? "Estudiante sin nombre" : name);
            }
            if (rows.Count < pageSize) break;
        }

        var result = new List<StudentGrammarReport>(students.Count);
        foreach (var (userId, name) in students.OrderBy(entry => entry.Value).ThenBy(entry => entry.Key))
        {
            // Se usa la misma intersección curso-estudiante que en las consultas individuales.
            var scope = new GrammarReportQuery { ClassId = course.Id, UserId = userId };
            var summary = await reports.Summary(scope);
            var categories = summary.TotalErrors == 0 ? [] : await reports.Categories(scope);
            result.Add(new(name, summary, categories));
        }
        return result;
    }

    private async Task<Dictionary<Guid, ClassEntity?>> FindPreviousCourses(
        IEnumerable<GrammarReportComparisonDto> comparison)
    {
        var courses = new Dictionary<Guid, ClassEntity?>();
        foreach (var id in comparison.SelectMany(GrammarReportExcelService.PreviousIds).Distinct())
            courses[id] = await FindCourse(id);
        return courses;
    }

    private async Task<(GrammarReportQuery Scope, ClassEntity? Course, string StudentName)> Prepare(
        ClaimsPrincipal user, GrammarReportQuery request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new ValidationContext(request), true);
        if (user.Identity?.IsAuthenticated != true
            || !Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var actor) || actor == Guid.Empty)
            throw new UnauthorizedAccessException();
        var coordinator = user.IsInRole("Coordinator");
        var teacher = user.IsInRole("Teacher");
        if (!coordinator && !teacher && !user.IsInRole("Student")) Denied();
        var classId = request.ClassId;
        var target = request.UserId;
        var reportScope = request.ReportScope?.Trim().ToLowerInvariant() switch
        {
            null => null,
            "class" => "Class",
            "personal" => "Personal",
            _ => throw new ValidationException("ReportScope debe ser Class o Personal.")
        };
        if (reportScope == "Class")
        {
            if (classId is null) throw new ValidationException("ClassId es obligatorio para el alcance Class.");
            if (!coordinator && !teacher) Denied();
            target = null;
        }
        if (reportScope == "Personal")
        {
            if (target is null) throw new ValidationException("UserId es obligatorio para el alcance Personal.");
            classId = null;
        }
        if (classId is null)
        {
            if (target is null) throw new ValidationException("Indica ClassId o UserId.");
            if (!coordinator && target != actor) Denied();
            var name = target == actor ? user.FindFirst(ClaimTypes.Name)?.Value : null;
            return (new() { UserId = target, ReportScope = reportScope }, null,
                string.IsNullOrWhiteSpace(name) ? "Usuario" : name);
        }
        if (!coordinator && !teacher)
        {
            if (target.HasValue && target != actor) Denied();
            target = actor;
        }
        var course = await FindCourse(classId.Value);
        if (course is null || (!coordinator && teacher && course.TeacherId != actor)) Denied();
        var studentName = "Grupo completo";
        if (target.HasValue)
        {
            // Sin filtro Active: una inscripción aprobada histórica sigue siendo elegible.
            RegistrationListItemDto? registration = null;
            for (var page = 1; ; page++)
            {
                var rows = await registrations.ListSummary(new PaginationDto<RegistrationSummaryList>
                {
                    PageNumber = page, PageSize = 100,
                    Filter = new() { UserId = target, Status = "Approved" }
                });
                registration = rows.FirstOrDefault(r => r.UserId == target && r.ClassId == classId
                    && string.Equals(r.Status, "Approved", StringComparison.OrdinalIgnoreCase));
                if (registration is not null || rows.Count < 100) break;
            }
            if (registration is null) Denied();
            studentName = $"{registration.FirstName} {registration.LastName}".Trim();
            if (studentName.Length == 0) studentName = "Estudiante sin nombre";
        }
        // La base resuelve el historial de los mismos estudiantes; no se eligen cursos anteriores en la API.
        return (new GrammarReportQuery { ClassId = classId, UserId = target, ReportScope = reportScope }, course, studentName);
    }

    private async Task<ClassEntity?> FindCourse(Guid id)
    {
        var courses = await classes.List(new PaginationDto<ClassList>
        {
            PageNumber = 1, PageSize = 2, Filter = new() { Id = id }
        });
        return courses.SingleOrDefault(course => course.Id == id);
    }

    [DoesNotReturn]
    private static void Denied() => throw new ReportAccessDeniedException("No tienes acceso al curso o estudiante solicitado.");
}
