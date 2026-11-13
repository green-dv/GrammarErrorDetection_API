using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class AchievementCollectionInsert
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; } = true;
}
