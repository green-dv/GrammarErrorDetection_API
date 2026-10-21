using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Evaluations;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class EvaluationRepository : IEvaluationRepository
{
    private readonly IStoredProcedureExecutor executor;

    public EvaluationRepository(IStoredProcedureExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);
        this.executor = executor;
    }

    public async Task<List<EvaluationEntity>> List(PaginationDto<EvaluationList> request)
    {
        RepositoryValidation.ValidatePagination(request);
        var filter = request.Filter ?? new EvaluationList();
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Evaluation_List",
            [
                new("Id", NpgsqlDbType.Uuid, filter.Id),
                new("RegistrationId", NpgsqlDbType.Uuid, filter.RegistrationId),
                new("UserId", NpgsqlDbType.Uuid, filter.UserId),
                new("Period", NpgsqlDbType.Integer, filter.Period),
                new("WritingScore", NpgsqlDbType.Integer, filter.WritingScore),
                new("GrammarScore", NpgsqlDbType.Integer, filter.GrammarScore),
                new("VocabularyScore", NpgsqlDbType.Integer, filter.VocabularyScore),
                new("ReadingScore", NpgsqlDbType.Integer, filter.ReadingScore),
                new("ListeningScore", NpgsqlDbType.Integer, filter.ListeningScore),
                new("OralScore", NpgsqlDbType.Integer, filter.OralScore),
                new("AcademicPerformance", NpgsqlDbType.Integer, filter.AcademicPerformance),
                new("CreatedAt", NpgsqlDbType.Date, filter.CreatedAt),
                new("Active", NpgsqlDbType.Boolean, filter.Active),
                new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
                new("PageSize", NpgsqlDbType.Integer, request.PageSize),
                new("Results", NpgsqlDbType.Refcursor, "repository_list", ParameterDirection.InputOutput)
            ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(ConvertToEntity.EvaluationListToEntity).ToList();
    }

    public async Task<EvaluationEntity> Insert(EvaluationInsert request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Evaluation_Insert",
            [
                new("RegistrationId", NpgsqlDbType.Uuid, request.RegistrationId),
                new("Period", NpgsqlDbType.Integer, request.Period),
                new("WritingScore", NpgsqlDbType.Integer, request.WritingScore),
                new("GrammarScore", NpgsqlDbType.Integer, request.GrammarScore),
                new("VocabularyScore", NpgsqlDbType.Integer, request.VocabularyScore),
                new("ReadingScore", NpgsqlDbType.Integer, request.ReadingScore),
                new("ListeningScore", NpgsqlDbType.Integer, request.ListeningScore),
                new("OralScore", NpgsqlDbType.Integer, request.OralScore),
                new("AcademicPerformance", NpgsqlDbType.Integer, request.AcademicPerformance),
                new("Active", NpgsqlDbType.Boolean, request.Active),
                new("Evaluation", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
            ], StoredProcedureResult.Composite("Evaluation", "grammar", "Evaluation"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return ConvertToEntity.EvaluationToEntity(table.Rows[0]);
    }

    public async Task<EvaluationEntity> Update(EvaluationUpdate request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Evaluation_Update",
            [
                new("Id", NpgsqlDbType.Uuid, request.Id),
                new("RegistrationId", NpgsqlDbType.Uuid, request.RegistrationId),
                new("Period", NpgsqlDbType.Integer, request.Period),
                new("WritingScore", NpgsqlDbType.Integer, request.WritingScore),
                new("GrammarScore", NpgsqlDbType.Integer, request.GrammarScore),
                new("VocabularyScore", NpgsqlDbType.Integer, request.VocabularyScore),
                new("ReadingScore", NpgsqlDbType.Integer, request.ReadingScore),
                new("ListeningScore", NpgsqlDbType.Integer, request.ListeningScore),
                new("OralScore", NpgsqlDbType.Integer, request.OralScore),
                new("AcademicPerformance", NpgsqlDbType.Integer, request.AcademicPerformance),
                new("Active", NpgsqlDbType.Boolean, request.Active),
                new("Evaluation", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
            ], StoredProcedureResult.Composite("Evaluation", "grammar", "Evaluation"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return ConvertToEntity.EvaluationToEntity(table.Rows[0]);
    }
}
