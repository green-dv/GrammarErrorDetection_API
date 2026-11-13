using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class AchievementInsert : IValidatableObject
{
    [Required] [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;
    [NotEmptyGuid]
    public Guid AchievementCategoryId { get; set; }
    [StringLength(255)]
    public string? IconLink { get; set; }
    [Range(0, int.MaxValue)]
    public int Points { get; set; } = 0;
    public bool IsSecret { get; set; } = false;
    public bool Active { get; set; } = true;
    [NotEmptyGuid]
    public Guid? AchievementCollectionId { get; set; }
    [Range(1, int.MaxValue)]
    public int? TierOrder { get; set; }
    /// <summary>Se obtiene cuando se cumplen todas (al menos una, hasta 10).</summary>
    [Required]
    public List<GamificationConditionInput> Conditions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (AchievementCollectionId.HasValue != TierOrder.HasValue)
            yield return new ValidationResult("AchievementCollectionId y TierOrder deben enviarse juntos.", [nameof(AchievementCollectionId), nameof(TierOrder)]);
        if (Conditions is null || Conditions.Count == 0)
            yield return new ValidationResult("El logro requiere al menos una condición.", [nameof(Conditions)]);
        foreach (var result in GamificationConditionInput.ValidateList(Conditions, nameof(Conditions)))
            yield return result;
    }
}
