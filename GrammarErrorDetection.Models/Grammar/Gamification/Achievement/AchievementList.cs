using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class AchievementList
{
    [NotEmptyGuid]
    public Guid? AchievementCollectionId { get; set; }

    [Range(1, int.MaxValue)]
    public int? TierOrder { get; set; }

    [NotEmptyGuid]
    public Guid? Id { get; set; }

    [StringLength(100)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    [StringLength(255)]
    public string? IconLink { get; set; }

    [NotEmptyGuid]
    /// <summary>Logros con al menos una condición de este tipo.</summary>
    public Guid? ConditionTypeId { get; set; }

    [NotEmptyGuid]
    public Guid? AchievementCategoryId { get; set; }

    [Range(0, int.MaxValue)]
    public int? Points { get; set; }

    public bool? IsSecret { get; set; }

    public bool? Active { get; set; }

    public DateOnly? CreatedAt { get; set; }

    [NotEmptyGuid]
    public Guid? CreatedByUserId { get; set; }
}
