namespace GrammarErrorDetection.Models.Grammar.Gamification;

// Las condiciones viven en GamificationCondition (ver AchievementListItem.Conditions).
public class AchievementEntity
{
    public Guid Id { get; set; }
    public Guid? AchievementCollectionId { get; set; }
    public int? TierOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid AchievementCategoryId { get; set; }
    public string? IconLink { get; set; }
    public int Points { get; set; }
    public bool IsSecret { get; set; }
    public bool Active { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
