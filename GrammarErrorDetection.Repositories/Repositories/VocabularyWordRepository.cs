using System.ComponentModel.DataAnnotations;
using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.Grammar.Vocabulary;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class VocabularyWordRepository : IVocabularyWordRepository
{
    /// <summary>Límite del SP VocabularyWord_GetMany por consulta.</summary>
    public const int MaxWordsPerQuery = 500;

    private readonly IStoredProcedureExecutor executor;

    public VocabularyWordRepository(IStoredProcedureExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);
        this.executor = executor;
    }

    public async Task<List<VocabularyWordEntity>> GetMany(IReadOnlyCollection<string> words, int maxAgeDays = 180)
    {
        ArgumentNullException.ThrowIfNull(words);
        if (maxAgeDays < 1) throw new ValidationException("maxAgeDays debe ser positivo.");
        if (words.Count > MaxWordsPerQuery)
            throw new ValidationException($"Se admiten como máximo {MaxWordsPerQuery} palabras por consulta.");
        if (words.Count == 0) return [];

        var response = await executor.ExecuteStoredProcedureAsync("grammar", "VocabularyWord_GetMany",
            [
                new("Words", NpgsqlDbType.Array | NpgsqlDbType.Text, words.ToArray()),
                new("Results", NpgsqlDbType.Refcursor, "vocabulary_word_list", ParameterDirection.InputOutput),
                new("MaxAgeDays", NpgsqlDbType.Integer, maxAgeDays)
            ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(ConvertToEntity.VocabularyWordToEntity).ToList();
    }

    public async Task<VocabularyWordEntity> Upsert(VocabularyWordUpsert request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "VocabularyWord_Upsert",
            [
                new("Word", NpgsqlDbType.Varchar, request.Word),
                new("IsValid", NpgsqlDbType.Boolean, request.IsValid),
                new("VocabularyWord", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output),
                new("Suggestions", NpgsqlDbType.Jsonb, VocabularyJson.Serialize(request.Suggestions))
            ], StoredProcedureResult.Composite("VocabularyWord", "grammar", "VocabularyWord"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return ConvertToEntity.VocabularyWordToEntity(table.Rows[0]);
    }
}
