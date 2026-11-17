using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Reports;
using GrammarErrorDetection.Services.Utils;

namespace GrammarErrorDetection.Services.Services;

internal sealed record StudentGrammarReport(string Name, GrammarReportSummaryDto Summary,
    IReadOnlyList<GrammarReportCategoryDto> Categories);

public sealed class GrammarReportExcelService
{
    internal GrammarReportFile Create(GrammarReportData data, ClassEntity? course, string student,
        IReadOnlyDictionary<Guid, ClassEntity?> previousCourses, IReadOnlyList<StudentGrammarReport> students)
    {
        var notes = Notes(course, student);
        var tables = new List<ExcelTable>
        {
            new("Resumen", course is null ? "Resumen personal" : "Resumen del curso", notes,
                ["Inscritos", "Estudiantes con análisis", "Cobertura (%)", "Textos enviados", "Textos analizados",
                    "Oraciones analizadas", "Errores", "Errores por texto enviado", "Errores por texto analizado"],
                [[data.Summary.EnrolledStudentCount, data.Summary.StudentsWithAnalyzedTexts, data.Summary.StudentCoveragePercentage,
                    data.Summary.TotalTexts, data.Summary.AnalyzedTexts, data.Summary.TotalSentences, data.Summary.TotalErrors,
                    data.Summary.ErrorsPerTextTotal, data.Summary.ErrorsPerAnalyzedText]]),
            new("Categorias", "Errores por categoría", notes,
                ["Categoría", "Errores", "Textos con el error", "Oraciones con el error", "Parte del total de errores (%)", "Errores por texto analizado"],
                data.Categories.Select<GrammarReportCategoryDto, object?[]>(row =>
                    [row.ErrorClassificationName, row.ErrorCount, row.RelatedTextCount, row.RelatedSentenceCount,
                        row.ErrorPercentage, row.ErrorsPerAnalyzedText])),
            new("Comparacion", "Comparación de errores", notes,
                ["Categoría", "Módulo anterior", "Paralelo anterior", "Año anterior", "Tendencia", "Errores actuales", "Errores anteriores", "Cambio de errores", "Cambio de errores (%)",
                    "Textos con errores actuales", "Textos con errores anteriores", "Errores por texto actual", "Errores por texto anterior",
                    "Cambio por texto", "Cambio por texto (%)"],
                data.Comparison.Select<GrammarReportComparisonDto, object?[]>(row =>
                    [Category(row), .. PreviousColumns(row, previousCourses), Trend(row.Trend), row.CurrentErrorCount, row.PreviousErrorCount,
                        row.ErrorCountChange, row.ErrorCountChangePercentage, row.CurrentRelatedTextCount, row.PreviousRelatedTextCount,
                        row.CurrentErrorsPerAnalyzedText, row.PreviousErrorsPerAnalyzedText, row.ErrorsPerAnalyzedTextChange,
                        row.ErrorsPerAnalyzedTextChangePercentage])),
            new("Afectados", "Estudiantes afectados por categoría", notes,
                ["Categoría", "Estudiantes afectados", "Estudiantes inscritos", "Afectados de inscritos (%)",
                    "Estudiantes con análisis", "Afectados de analizados (%)"],
                data.Categories.Select<GrammarReportCategoryDto, object?[]>(row =>
                    [row.ErrorClassificationName, row.AffectedStudentCount, row.EnrolledStudentCount, row.AffectedStudentPercentage,
                        row.StudentsWithAnalyzedTexts, row.AffectedAnalyzedStudentPercentage])),
            new("Cobertura", "Cobertura y disponibilidad de historial", notes,
                ["Alcance", "Inscritos", "Estudiantes con análisis", "Textos enviados", "Textos analizados",
                    "Estudiantes con curso anterior", "Estudiantes sin curso anterior"],
                data.Comparison.Where(row => row.Scope == "Overall").SelectMany<GrammarReportComparisonDto, object?[]>(row =>
                    [
                        ["Actual", row.CurrentEnrolledStudentCount, row.CurrentStudentsWithAnalyzedTexts, row.CurrentTotalTexts,
                            row.CurrentAnalyzedTexts, row.StudentsWithPreviousClassCount, row.StudentsWithoutPreviousClassCount],
                        ["Anterior", row.PreviousEnrolledStudentCount, row.PreviousStudentsWithAnalyzedTexts, row.PreviousTotalTexts,
                            row.PreviousAnalyzedTexts, null, null]
                    ]))
        };

        if (course is null)
            tables.RemoveAll(table => table.Name is "Comparacion" or "Cobertura");
        else
            tables.Add(StudentTable(notes, data.Categories, students));

        return new(ExcelWorkbook.Create([.. tables]), FileName("reporte-gramatical", course, student));
    }

    private static ExcelTable StudentTable(string[] notes, IReadOnlyList<GrammarReportCategoryDto> courseCategories,
        IReadOnlyList<StudentGrammarReport> students)
    {
        var categories = courseCategories.Concat(students.SelectMany(student => student.Categories))
            .DistinctBy(category => category.ErrorClassificationId)
            .OrderBy(category => category.ErrorClassificationName)
            .ThenBy(category => category.ErrorClassificationId)
            .ToList();

        return new("Errores por estudiante", "Errores gramaticales por estudiante", notes,
            ["Estudiante", "Textos analizados", "Errores totales", "Errores por texto analizado",
                .. categories.Select(category => category.ErrorClassificationName)],
            students.Select(student =>
            {
                var counts = student.Categories.ToDictionary(category => category.ErrorClassificationId, category => category.ErrorCount);
                return new object?[]
                {
                    student.Name, student.Summary.AnalyzedTexts, student.Summary.TotalErrors, student.Summary.ErrorsPerAnalyzedText
                }.Concat(categories.Select(category => student.Summary.AnalyzedTexts == 0 ? null
                    : (object)counts.GetValueOrDefault(category.ErrorClassificationId))).ToArray();
            }));
    }

    private static string[] Notes(ClassEntity? course, string student) => course is null
        ? ["Alcance personal", $"Usuario · {student}"]
        : [CourseName(course), $"Estudiante · {student}"];

    private static string CourseName(ClassEntity course) =>
        $"{ModuleName(course)} · Paralelo {course.Parallel ?? "sin dato"} · Bimestre {course.Bimester} · {course.Year}";

    private static string ModuleName(ClassEntity course) =>
        string.IsNullOrWhiteSpace(course.ModuleName) ? "Módulo sin nombre" : course.ModuleName;

    private static string Category(GrammarReportComparisonDto row) =>
        row.Scope == "Overall" ? "Total general" : row.ErrorClassificationName ?? "Categoría sin nombre";

    internal static IEnumerable<Guid> PreviousIds(GrammarReportComparisonDto row) =>
        row.PreviousClassIds.Length > 0 ? row.PreviousClassIds
            : row.PreviousClassId is Guid id ? [id] : [];

    private static object?[] PreviousColumns(GrammarReportComparisonDto row,
        IReadOnlyDictionary<Guid, ClassEntity?> previousCourses)
    {
        var courses = PreviousIds(row).Distinct().Select(id => previousCourses[id]).ToArray();
        if (courses.Length == 0) return [null, null, null];
        return
        [
            string.Join(" / ", courses.Select(course => course is null ? "Sin datos" : ModuleName(course))),
            string.Join(" / ", courses.Select(course => course?.Parallel ?? "—")),
            courses.Length == 1 ? (object?)courses[0]?.Year
                : string.Join(" / ", courses.Select(course => course?.Year.ToString() ?? "—"))
        ];
    }

    private static string Trend(string value) => value switch
    {
        "Improved" => "Mejora",
        "Worsened" => "Empeora",
        "Unchanged" => "Sin cambio",
        "InsufficientData" => "Datos insuficientes",
        _ => value
    };

    private static string FileName(string prefix, ClassEntity? course, string student) =>
        course is null ? $"{prefix}-personal-{FilePart(student)}.xlsx"
        : $"{prefix}-{FilePart(ModuleName(course))}-paralelo-{FilePart(course.Parallel ?? "sin dato")}"
        + $"-bimestre-{course.Bimester}-{course.Year}-{FilePart(student)}.xlsx";

    private static string FilePart(string text)
    {
        const string invalid = "<>:\"/\\|?*";
        return new string(text.Trim().Take(45).Select(c => char.IsControl(c) || invalid.Contains(c) ? '-' : c).ToArray())
            .TrimEnd('.', ' ');
    }
}
