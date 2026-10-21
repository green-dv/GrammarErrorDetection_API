using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.Grammar.Sentences;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class SentenceRepository : ISentenceRepository
{
    private readonly IStoredProcedureExecutor executor;

    public SentenceRepository(IStoredProcedureExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);
        this.executor = executor;
    }

    public async Task<SentenceEntity> Insert(SentenceInsert request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Sentence_Insert",
            [
                new("TextId", NpgsqlDbType.Uuid, request.TextId),
                new("SentenceIndex", NpgsqlDbType.Integer, request.SentenceIndex),
                new("OriginalSentence", NpgsqlDbType.Text, request.OriginalSentence),
                new("Sentence", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output),
                new("CorrectedSentence", NpgsqlDbType.Text, request.CorrectedSentence),
                new("Active", NpgsqlDbType.Boolean, request.Active)
            ], StoredProcedureResult.Composite("Sentence", "grammar", "Sentence"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return ConvertToEntity.SentenceToEntity(table.Rows[0]);
    }
}
