using System.Data;
using System.Text.Json;
using GrammarErrorDetection.Models.Grammar.Vocabulary;

namespace GrammarErrorDetection.Repositories.Utils;

/// <summary>JSONB de VocabularyWord.Suggestions: se escribe y se lee siempre con los mismos nombres (camelCase).</summary>
internal static class VocabularyJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    internal static string Serialize(IEnumerable<VocabularyAlternative> suggestions)
        => JsonSerializer.Serialize(suggestions.ToList(), Options);

    internal static List<VocabularyAlternative> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<VocabularyAlternative>>(json, Options) ?? []; }
        catch (JsonException exception)
        {
            throw new DataException("Suggestions de VocabularyWord no tiene el formato esperado.", exception);
        }
    }
}
