using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class AchievementCategoryUpdate
{
    [NotEmptyGuid]
    public Guid Id { get; set; }

    [StringLength(100)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    public bool? Active { get; set; }
}
