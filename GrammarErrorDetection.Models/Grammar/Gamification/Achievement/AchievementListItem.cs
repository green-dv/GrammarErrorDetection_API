namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class AchievementListItem : AchievementEntity
{
    public string? AchievementCollectionName { get; set; }
    public string? AchievementCategoryName { get; set; }
    public List<GamificationConditionDto> Conditions { get; set; } = [];
}
