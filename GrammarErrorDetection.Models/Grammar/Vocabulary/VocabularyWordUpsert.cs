using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Vocabulary;

public sealed class VocabularyWordUpsert
{
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string Word { get; set; } = string.Empty;

    public bool IsValid { get; set; }

    /// <summary>Solo para palabras inválidas: una palabra válida se guarda sin sugerencias.</summary>
    public List<VocabularyAlternative> Suggestions { get; set; } = [];
}
