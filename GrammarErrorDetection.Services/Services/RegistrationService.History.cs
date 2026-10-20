using System.Data;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Evaluations;
using GrammarErrorDetection.Models.Grammar.Registrations;

namespace GrammarErrorDetection.Services.Services;

public sealed partial class RegistrationService
{
    public async Task<List<StudentCourseHistoryDto>> ListStudentCourses(Guid studentId, StudentCourseHistoryQuery request)
    {
        ValidateStudentId(studentId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateApplication(request);

        // El SP ordena por CreatedAt; se recorre el listado para ordenar por la
        // fecha real de inscripción antes de aplicar la página solicitada.
        var registrations = await ReadApplications(repository.ListSummary, new RegistrationSummaryList { UserId = studentId });
        var offset = ((long)request.PageNumber - 1) * request.PageSize;
        if (offset > int.MaxValue) return [];
        var selected = registrations.Where(r => r.UserId == studentId && r.ClassId.HasValue
                && !IsStatus(r.Status.Trim(), "Rejected") && !IsStatus(r.Status.Trim(), "Cancelled")
                && !IsStatus(r.Status.Trim(), "Canceled"))
            .OrderByDescending(r => r.RegistrationDate).ThenByDescending(r => r.UpdatedAt)
            .ThenByDescending(r => r.RegistrationId)
            .Skip((int)offset).Take(request.PageSize).ToList();

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
        return result;
    }
}
