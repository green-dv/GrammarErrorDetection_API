using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Texts;

public sealed class TextList
{
    [NotEmptyGuid]
    public Guid? Id { get; set; }

    [NotEmptyGuid]
    public Guid? UserId { get; set; }

    public DateOnly? SubmissionDate { get; set; }
    public string? OriginalContent { get; set; }
    [StringLength(30)]
    public string? Status { get; set; }
    public bool? Active { get; set; }
    public DateOnly? CreatedAt { get; set; }
}
