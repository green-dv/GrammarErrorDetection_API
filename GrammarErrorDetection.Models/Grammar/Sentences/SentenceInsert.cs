using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Sentences;

public sealed class SentenceInsert
{
    [NotEmptyGuid]
    public Guid TextId { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int? SentenceIndex { get; set; }

    [Required]
    public string OriginalSentence { get; set; } = string.Empty;

    public string? CorrectedSentence { get; set; }

    public bool Active { get; set; } = true;
}
