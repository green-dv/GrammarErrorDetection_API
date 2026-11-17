using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.ErrorClassifications;

public sealed class ErrorClassificationInsert
{
    [Required] [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool Active { get; set; } = true;
}
