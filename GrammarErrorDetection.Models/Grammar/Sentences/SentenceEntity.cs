namespace GrammarErrorDetection.Models.Grammar.Sentences;

public sealed class SentenceEntity
{
    public Guid Id { get; set; }
    public Guid TextId { get; set; }
    public int SentenceIndex { get; set; }
    public string OriginalSentence { get; set; } = string.Empty;
    public string? CorrectedSentence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Active { get; set; }
}
