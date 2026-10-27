using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.ErrorClassifications;

public sealed class ErrorClassificationList
{
    [NotEmptyGuid]
    public Guid? Id { get; set; }
    [StringLength(100)]
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? Active { get; set; }
    public DateOnly? CreatedAt { get; set; }
}
