using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class ConditionTypeInsert
{
    [Required] [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required] [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required] [StringLength(20)]
    public string ValueType { get; set; } = "Integer";

    public bool Active { get; set; } = true;
}
