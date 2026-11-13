using System.Data;
using System.Text.Json;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.Grammar.Reports;
using GrammarErrorDetection.Repositories.Interfaces;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class GrammarReportRepository(IStoredProcedureExecutor executor) : IGrammarReportRepository
{
    private async Task<DataTable> Read(string procedure, GrammarReportQuery request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ClassId == Guid.Empty || request.UserId == Guid.Empty || (request.ClassId is null && request.UserId is null))
            throw new ArgumentException("El alcance del reporte no es válido.");
        List<StoredProcedureParameter> parameters =
        [
            new("ClassId", NpgsqlDbType.Uuid, request.ClassId),
            new("UserId", NpgsqlDbType.Uuid, request.UserId),
            new("Results", NpgsqlDbType.Refcursor, "grammar_report", ParameterDirection.InputOutput)
        ];
        if (request.ReportScope is not null)
            parameters.Add(new("ReportScope", NpgsqlDbType.Text, request.ReportScope));
        var response = await executor.ExecuteStoredProcedureAsync("grammar", procedure, parameters, StoredProcedureResult.Cursor("Results"));
        return response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
    }

    public async Task<GrammarReportSummaryDto> Summary(GrammarReportQuery request)
    {
        using var table = await Read("GrammarReport_Summary", request);
        if (table.Rows.Count != 1) throw new DataException("Summary debe devolver exactamente una fila.");
        var rows = table.AsEnumerable().Select(row => new GrammarReportSummaryDto
        {
            ClassId = row.Field<Guid?>("ClassId"),
            UserId = row.Field<Guid?>("UserId"),
            EnrolledStudentCount = row.Field<long>("EnrolledStudentCount"),
            StudentsWithAnalyzedTexts = row.Field<long>("StudentsWithAnalyzedTexts"),
            TotalTexts = row.Field<long>("TotalTexts"),
            AnalyzedTexts = row.Field<long>("AnalyzedTexts"),
            TotalSentences = row.Field<long>("TotalSentences"),
            TotalErrors = row.Field<long>("TotalErrors"),
            StudentCoveragePercentage = row.Field<decimal?>("StudentCoveragePercentage"),
            ErrorsPerTextTotal = row.Field<decimal?>("ErrorsPerTextTotal"),
            ErrorsPerAnalyzedText = row.Field<decimal?>("ErrorsPerAnalyzedText"),
            SentencesWithErrors = row.Field<long>("SentencesWithErrors"),
            AccuracyPercentage = row.Field<decimal?>("AccuracyPercentage"),
        }).ToList();
        return rows[0];
    }

    public async Task<List<GrammarReportCategoryDto>> Categories(GrammarReportQuery request)
    {
        using var table = await Read("GrammarReport_Category_List", request);
        var rows = table.AsEnumerable().Select(row => new GrammarReportCategoryDto
        {
            ErrorClassificationId = row.Field<Guid>("ErrorClassificationId"),
            ErrorClassificationName = row.Field<string>("ErrorClassificationName") ?? throw new DataException("Falta ErrorClassificationName."),
            ErrorCount = row.Field<long>("ErrorCount"),
            RelatedTextCount = row.Field<long>("RelatedTextCount"),
            RelatedSentenceCount = row.Field<long>("RelatedSentenceCount"),
            AffectedStudentCount = row.Field<long>("AffectedStudentCount"),
            EnrolledStudentCount = row.Field<long>("EnrolledStudentCount"),
            StudentsWithAnalyzedTexts = row.Field<long>("StudentsWithAnalyzedTexts"),
            AffectedStudentPercentage = row.Field<decimal?>("AffectedStudentPercentage"),
            AffectedAnalyzedStudentPercentage = row.Field<decimal?>("AffectedAnalyzedStudentPercentage"),
            ErrorPercentage = row.Field<decimal?>("ErrorPercentage"),
            ErrorsPerAnalyzedText = row.Field<decimal?>("ErrorsPerAnalyzedText"),
        }).ToList();
        return rows;
    }

    public async Task<List<GrammarReportComparisonDto>> Comparison(GrammarReportQuery request)
    {
        using var table = await Read("GrammarReport_Comparison", request);
        var rows = table.AsEnumerable().Select(row => new GrammarReportComparisonDto
        {
            Scope = row.Field<string>("Scope") ?? throw new DataException("Falta Scope."),
            ComparisonStatus = row.Field<string>("ComparisonStatus") ?? throw new DataException("Falta ComparisonStatus."),
            Trend = row.Field<string>("Trend") ?? throw new DataException("Falta Trend."),
            ClassId = row.Field<Guid>("ClassId"),
            UserId = row.Field<Guid?>("UserId"),
            ErrorClassificationId = row.Field<Guid?>("ErrorClassificationId"),
            ErrorClassificationName = row.Field<string?>("ErrorClassificationName"),
            CurrentEnrolledStudentCount = row.Field<long>("CurrentEnrolledStudentCount"),
            PreviousEnrolledStudentCount = row.Field<long>("PreviousEnrolledStudentCount"),
            PreviousClassId = row.Field<Guid?>("PreviousClassId"),
            PreviousClassIds = ReadClassIds(row["PreviousClassIds"]),
            StudentsWithPreviousClassCount = row.Field<long>("StudentsWithPreviousClassCount"),
            StudentsWithoutPreviousClassCount = row.Field<long>("StudentsWithoutPreviousClassCount"),
            CurrentTotalTexts = row.Field<long>("CurrentTotalTexts"),
            CurrentAnalyzedTexts = row.Field<long>("CurrentAnalyzedTexts"),
            CurrentStudentsWithAnalyzedTexts = row.Field<long>("CurrentStudentsWithAnalyzedTexts"),
            CurrentErrorCount = row.Field<long>("CurrentErrorCount"),
            CurrentRelatedTextCount = row.Field<long>("CurrentRelatedTextCount"),
            CurrentAffectedStudentCount = row.Field<long>("CurrentAffectedStudentCount"),
            PreviousTotalTexts = row.Field<long>("PreviousTotalTexts"),
            PreviousAnalyzedTexts = row.Field<long>("PreviousAnalyzedTexts"),
            PreviousStudentsWithAnalyzedTexts = row.Field<long>("PreviousStudentsWithAnalyzedTexts"),
            PreviousErrorCount = row.Field<long>("PreviousErrorCount"),
            PreviousRelatedTextCount = row.Field<long>("PreviousRelatedTextCount"),
            PreviousAffectedStudentCount = row.Field<long>("PreviousAffectedStudentCount"),
            ErrorCountChange = row.Field<long?>("ErrorCountChange"),
            AffectedStudentCountChange = row.Field<long?>("AffectedStudentCountChange"),
            AffectedStudentPercentagePointChange = row.Field<decimal?>("AffectedStudentPercentagePointChange"),
            AffectedAnalyzedStudentPercentagePointChange = row.Field<decimal?>("AffectedAnalyzedStudentPercentagePointChange"),
            ErrorsPerAnalyzedTextChange = row.Field<decimal?>("ErrorsPerAnalyzedTextChange"),
            ErrorCountChangePercentage = row.Field<decimal?>("ErrorCountChangePercentage"),
            ErrorsPerAnalyzedTextChangePercentage = row.Field<decimal?>("ErrorsPerAnalyzedTextChangePercentage"),
            CurrentAffectedStudentPercentage = row.Field<decimal?>("CurrentAffectedStudentPercentage"),
            CurrentAffectedAnalyzedStudentPercentage = row.Field<decimal?>("CurrentAffectedAnalyzedStudentPercentage"),
            CurrentErrorsPerAnalyzedText = row.Field<decimal?>("CurrentErrorsPerAnalyzedText"),
            PreviousAffectedStudentPercentage = row.Field<decimal?>("PreviousAffectedStudentPercentage"),
            PreviousAffectedAnalyzedStudentPercentage = row.Field<decimal?>("PreviousAffectedAnalyzedStudentPercentage"),
            PreviousErrorsPerAnalyzedText = row.Field<decimal?>("PreviousErrorsPerAnalyzedText"),
        }).ToList();
        return rows;
    }
    private static Guid[] ReadClassIds(object value)
    {
        try
        {
            return value switch
            {
                DBNull => [],
                Guid[] ids => ids,
                string json => JsonSerializer.Deserialize<Guid[]>(json) ?? [],
                JsonDocument document => document.RootElement.Deserialize<Guid[]>() ?? [],
                JsonElement element => element.Deserialize<Guid[]>() ?? [],
                _ => throw new DataException("PreviousClassIds debe ser un arreglo UUID o JSON.")
            };
        }
        catch (JsonException exception)
        {
            throw new DataException("PreviousClassIds contiene un UUID o JSON inválido.", exception);
        }
    }
}
