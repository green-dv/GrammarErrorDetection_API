using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class ConditionTypeUpdate
{
    [NotEmptyGuid]
    public Guid Id { get; set; }

    [StringLength(50)]
    public string? Code { get; set; }

    [StringLength(100)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    [StringLength(20)]
    public string? ValueType { get; set; }

    public bool? Active { get; set; }
}
