using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Grammar.Texts;

public sealed class SentenceErrorDto
{
    public Guid Id { get; set; }
    public Guid TextId { get; set; }
    public int SentenceIndex { get; set; }

    [JsonRequired]
    public string OriginalSentence { get; set; } = string.Empty;

    public string? CorrectedSentence { get; set; }
    public List<SentenceErrorDetailDto> Errors { get; set; } = [];
    public int ErrorCount => Errors.Count;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool? Active { get; set; }
}

public sealed class SentenceErrorDetailDto
{
    public Guid Id { get; set; }
    public Guid SentenceId { get; set; }
    public Guid ErrorClassificationId { get; set; }

    [JsonRequired]
    public string ErrorClassificationName { get; set; } = string.Empty;

    public string? ErrorToken { get; set; }
    public int ErrorTokenStartIndex { get; set; }
    public int ErrorTokenEndIndex { get; set; }
    public string? SuggestedCorrection { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool? Active { get; set; }
}
