namespace GrammarErrorDetection.Models.Grammar.Vocabulary;

/// <summary>Fila de la caché grammar."VocabularyWord": el resultado ya verificado de una palabra.</summary>
public sealed class VocabularyWordEntity
{
    public Guid Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<VocabularyAlternative> Suggestions { get; set; } = [];
    public DateTime FetchedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Active { get; set; }
}
