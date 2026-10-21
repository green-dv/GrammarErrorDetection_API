using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.SentenceErrors;

public sealed class SentenceErrorInsert
{
    [NotEmptyGuid]
    public Guid SentenceId { get; set; }

    [NotEmptyGuid]
    public Guid ErrorClassificationId { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int? ErrorTokenStartIndex { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int? ErrorTokenEndIndex { get; set; }

    [StringLength(255)]
    public string? ErrorToken { get; set; }

    [StringLength(255)]
    public string? SuggestedCorrection { get; set; }

    [Range(typeof(decimal), "-999.99", "999.99")]
    public decimal? ConfidenceScore { get; set; }

    public bool Active { get; set; } = true;
}
