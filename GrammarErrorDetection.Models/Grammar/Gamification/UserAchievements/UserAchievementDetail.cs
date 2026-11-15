namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class UserAchievementDetail : AchievementEntity
{
    public string? AchievementCategoryName { get; set; }
    public List<GamificationConditionDto> Conditions { get; set; } = [];
    public DateTime? ObtainedAt { get; set; }
}
