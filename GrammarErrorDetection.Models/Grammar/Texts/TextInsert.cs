using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Texts;

public sealed class TextInsert
{
    [NotEmptyGuid]
    public Guid UserId { get; set; }

    public DateTime? SubmissionDate { get; set; }

    [Required]
    public string OriginalContent { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Pendiente";

    public bool Active { get; set; } = true;
}
