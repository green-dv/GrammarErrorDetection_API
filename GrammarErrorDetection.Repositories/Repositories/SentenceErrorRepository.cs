using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.Grammar.SentenceErrors;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class SentenceErrorRepository : ISentenceErrorRepository
{
    private readonly IStoredProcedureExecutor executor;

    public SentenceErrorRepository(IStoredProcedureExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);
        this.executor = executor;
    }

    public async Task<SentenceErrorEntity> Insert(SentenceErrorInsert request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "SentenceError_Insert",
            [
                new("SentenceId", NpgsqlDbType.Uuid, request.SentenceId),
                new("ErrorClassificationId", NpgsqlDbType.Uuid, request.ErrorClassificationId),
                new("ErrorTokenStartIndex", NpgsqlDbType.Integer, request.ErrorTokenStartIndex),
                new("ErrorTokenEndIndex", NpgsqlDbType.Integer, request.ErrorTokenEndIndex),
                new("SentenceError", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output),
                new("ErrorToken", NpgsqlDbType.Varchar, request.ErrorToken),
                new("SuggestedCorrection", NpgsqlDbType.Varchar, request.SuggestedCorrection),
                new("ConfidenceScore", NpgsqlDbType.Numeric, request.ConfidenceScore),
                new("Active", NpgsqlDbType.Boolean, request.Active)
            ], StoredProcedureResult.Composite("SentenceError", "grammar", "SentenceError"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return ConvertToEntity.SentenceErrorToEntity(table.Rows[0]);
    }
}
