using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Texts;

public sealed class TextUpdate
{
    [NotEmptyGuid]
    public Guid Id { get; set; }

    [NotEmptyGuid]
    public Guid? UserId { get; set; }

    public DateTime? SubmissionDate { get; set; }
    public string? OriginalContent { get; set; }
    [StringLength(30)]
    public string? Status { get; set; }
    public bool? Active { get; set; }
}
