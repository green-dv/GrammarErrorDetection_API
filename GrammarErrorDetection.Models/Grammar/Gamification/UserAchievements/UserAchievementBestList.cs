using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class UserAchievementBestList
{
    [NotEmptyGuid]
    public Guid? AchievementCollectionId { get; set; }
    [NotEmptyGuid]
    public Guid? AchievementCategoryId { get; set; }
}
