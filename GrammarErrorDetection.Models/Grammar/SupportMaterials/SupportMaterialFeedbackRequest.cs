using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.SupportMaterials;

public sealed class SupportMaterialFeedbackRequest
{
    [Required]
    public bool? Helpful { get; set; }

    // Solo se guarda cuando Helpful = false.
    [StringLength(500)]
    public string? Comment { get; set; }
}
