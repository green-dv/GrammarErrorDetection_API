namespace GrammarErrorDetection.Models.Grammar.SentenceErrors;

public sealed class SentenceErrorEntity
{
    public Guid Id { get; set; }
    public Guid SentenceId { get; set; }
    public Guid ErrorClassificationId { get; set; }
    public string? ErrorToken { get; set; }
    public int ErrorTokenStartIndex { get; set; }
    public int ErrorTokenEndIndex { get; set; }
    public string? SuggestedCorrection { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Active { get; set; }
}
