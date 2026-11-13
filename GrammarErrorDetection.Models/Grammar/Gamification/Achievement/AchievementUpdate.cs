using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class AchievementUpdate : IValidatableObject
{
    [NotEmptyGuid]
    public Guid Id { get; set; }
    [StringLength(100)]
    public string? Name { get; set; }
    public string? Description { get; set; }
    [StringLength(255)]
    public string? IconLink { get; set; }
    [NotEmptyGuid]
    public Guid? AchievementCategoryId { get; set; }
    [Range(0, int.MaxValue)]
    public int? Points { get; set; }
    public bool? IsSecret { get; set; }
    public bool? Active { get; set; }
    [NotEmptyGuid]
    public Guid? AchievementCollectionId { get; set; }
    [Range(1, int.MaxValue)]
    public int? TierOrder { get; set; }
    public bool ClearCollection { get; set; } = false;
    /// <summary>null conserva las condiciones; una lista las reemplaza (no permitido si alguien ya obtuvo el logro).</summary>
    public List<GamificationConditionInput>? Conditions { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Conditions is { Count: 0 })
            yield return new ValidationResult("El logro requiere al menos una condición.", [nameof(Conditions)]);
        foreach (var result in GamificationConditionInput.ValidateList(Conditions, nameof(Conditions)))
            yield return result;
    }
}
