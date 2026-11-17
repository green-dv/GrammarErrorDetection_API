using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.ErrorClassifications;

/// <summary>Solo se actualizan los campos enviados; Active = false deshabilita la categoría.</summary>
public sealed class ErrorClassificationUpdate
{
    [NotEmptyGuid]
    public Guid Id { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    public bool? Active { get; set; }
}
