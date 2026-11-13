namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class UserAchievementBestItem : AchievementEntity
{
    public string? AchievementCollectionName { get; set; }
    public string? AchievementCollectionDescription { get; set; }
    public string? AchievementCategoryName { get; set; }
    public string? ConditionCode { get; set; }
    public string? ConditionName { get; set; }
    // ConditionCode/ConditionName/CurrentValue/Next* describen la primera condición pendiente;
    // ProgressPercent, NextConditionMet y CanReachNext consideran todas las condiciones.
    public int ConditionCount { get; set; }
    public List<GamificationConditionProgress> Conditions { get; set; } = [];
    public bool IsObtained { get; set; }
    public bool IsMaxTier { get; set; }
    public bool HasNextAchievement { get; set; }
    public Guid? UserAchievementId { get; set; }
    public DateTime? ObtainedAt { get; set; }
    public Guid? PointTransactionId { get; set; }
    public int? AwardPoints { get; set; }
    public UserAchievementDetail? BestAchievement { get; set; }
    public UserAchievementDetail? NextAchievement { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? NextTargetValue { get; set; }
    public string? NextComparisonOperator { get; set; }
    public decimal? NextRequiredValue { get; set; }
    public decimal? RemainingValue { get; set; }
    public decimal? ProgressPercent { get; set; }
    public bool? NextConditionMet { get; set; }
    public bool? CanReachNext { get; set; }
    public long TotalCount { get; set; }
}
