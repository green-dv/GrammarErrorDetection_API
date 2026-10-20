using System.ComponentModel.DataAnnotations;
using System.Data;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Classrooms;
using GrammarErrorDetection.Models.Grammar.Evaluations;
using GrammarErrorDetection.Models.Grammar.ModuleNames;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Services;

internal static class CourseHistoryChecks
{
    public static async Task Run()
    {
        var checks = 0;
        var studentId = Guid.NewGuid();
        var course = new ClassEntity
        {
            Id = Guid.NewGuid(), ModuleNameId = Guid.NewGuid(), ModuleName = "Inglés II",
            TeacherName = "María Pérez", Parallel = "B", StartTime = new(18, 0), EndTime = new(20, 0),
            Year = 2026, Bimester = 4, Active = false
        };
        var courses = new List<ClassEntity> { course };
        var records = new Registrations();
        var older = Registration(new(2026, 7, 1));
        var newer = Registration(new(2026, 9, 1));
        var undated = Registration(null);
        records.Data.AddRange([older, undated, newer,
            Registration(new(2026, 10, 1), "Rejected"), Registration(new(2026, 10, 2), " Cancelled "),
            new RegistrationEntity { Id = Guid.NewGuid(), UserId = studentId },
            new RegistrationEntity { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), ClassId = course.Id }]);
        var incomplete = Grade(newer.Id, 2, 80);
        incomplete.OralScore = null;
        var grades = new List<EvaluationEntity>
        {
            Grade(newer.Id, 1, 70), incomplete, Grade(older.Id, 1, 0), Grade(older.Id, 2, 80)
        };
        var service = new RegistrationService(records,
            ListProxy.Create<IClassRepository, ClassList, ClassEntity>(courses),
            ListProxy.Create<IClassroomRepository, ClassroomList, ClassroomEntity>([]),
            ListProxy.Create<IModuleNameRepository, ModuleNameList, ModuleNameEntity>([]),
            ListProxy.Create<IEvaluationRepository, EvaluationList, EvaluationEntity>(grades));
        var history = await service.ListStudentCourses(studentId, new());
        Check(history.Select(c => c.RegistrationId).SequenceEqual([newer.Id, older.Id, undated.Id]),
            "Ordena por fecha de inscripción descendente, fechas nulas al final, excluyendo rechazadas, canceladas y registros sin curso.");
        Check(history[0].TeacherName == "María Pérez" && history[0].Parallel == "B"
            && history[0].StartTime == new TimeOnly(18, 0) && history[0].EndTime == new TimeOnly(20, 0),
            "Devuelve docente, paralelo y horario desde Class_List, incluso en cursos históricos inactivos.");
        Check(history[0].Exam1Score == 70 && history[0].Exam2Score is null,
            "Devuelve un examen completo aunque el otro no esté calificado por completo.");
        Check(history[1].Exam1Score == 0 && history[1].Exam2Score == 80,
            "Conserva la nota cero y no confunde las evaluaciones de diferentes inscripciones.");
        Check(history[2].Exam1Score is null && history[2].Exam2Score is null,
            "Sin evaluaciones las notas son null.");
        Check(history[0].ClassId == course.Id && history[0].ModuleId == course.ModuleNameId
            && history[0].ModuleName == course.ModuleName && history[0].Year == 2026 && history[0].Bimester == 4,
            "Identifica curso, módulo y periodo.");
        Check(records.LastSummaryUserId == studentId, "Consulta Registration_List con el filtro del estudiante.");
        history = await service.ListStudentCourses(studentId, new() { PageNumber = 2, PageSize = 1 });
        Check(history.Single().RegistrationId == older.Id, "Pagina después de ordenar por fecha de inscripción.");
        grades.Add(Grade(newer.Id, 1, 60));
        history = await service.ListStudentCourses(studentId, new());
        Check(history[0].Exam1Score is null, "No elige arbitrariamente entre evaluaciones activas duplicadas.");
        grades[^1].Active = false;
        grades[0].GrammarScore = 10;
        grades[0].VocabularyScore = 10;
        history = await service.ListStudentCourses(studentId, new());
        Check(history[0].Exam1Score is null, "Una evaluación fuera de rango no se muestra como nota válida.");
        Check((await service.ListStudentCourses(Guid.NewGuid(), new())).Count == 0, "Sin inscripciones devuelve una lista vacía.");
        await Reject(() => service.ListStudentCourses(studentId, new() { PageNumber = 0 }));
        await Reject(() => service.ListStudentCourses(Guid.Empty, new()));

        // La fecha de inscripción puede diferir del orden CreatedAt usado por el SP.
        records.Data.Clear();
        for (var day = 0; day < 205; day++)
            records.Data.Add(Registration(new DateTime(2025, 1, 1).AddDays(day)));
        history = await service.ListStudentCourses(studentId, new() { PageSize = 1 });
        Check(history.Single().RegistrationId == records.Data[^1].Id,
            "Recorre todas las páginas del SP antes de ordenar, incluyendo la inscripción 205.");
        courses.Clear();
        try
        {
            await service.ListStudentCourses(studentId, new());
            throw new Exception("No debe omitir silenciosamente un curso que falta en Class_List.");
        }
        catch (DataException) { checks++; }
        Console.WriteLine($"{checks} comprobaciones del historial de cursos correctas.");

        RegistrationEntity Registration(DateTime? date, string status = "Approved") => new()
        {
            Id = Guid.NewGuid(), UserId = studentId, ClassId = course.Id,
            RegistrationDate = date, UpdatedAt = DateTime.UtcNow, Status = status,
            // El historial no exige campos ausentes del cursor público.
            Active = null, CreatedAt = null
        };
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            checks++;
        }
        async Task Reject(Func<Task> action)
        {
            try { await action(); }
            catch (ValidationException) { checks++; return; }
            throw new Exception("Se aceptó un identificador o una paginación inválidos.");
        }
    }

    private static EvaluationEntity Grade(Guid registrationId, int period, int score) => new()
    {
        Id = Guid.NewGuid(), RegistrationId = registrationId, Period = period,
        WritingScore = Math.Min(score, 30), OralScore = Math.Clamp(score - 30, 0, 15),
        ListeningScore = Math.Clamp(score - 45, 0, 10), ReadingScore = Math.Clamp(score - 55, 0, 10),
        GrammarScore = Math.Clamp(score - 65, 0, 10), VocabularyScore = 0,
        AcademicPerformance = Math.Clamp(score - 75, 0, 25)
    };
}
