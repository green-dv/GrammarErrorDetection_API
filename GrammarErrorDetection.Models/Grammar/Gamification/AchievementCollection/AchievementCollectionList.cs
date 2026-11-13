using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class AchievementCollectionList
{
    [NotEmptyGuid]
    public Guid? Id { get; set; }
    [StringLength(150)]
    public string? Name { get; set; }
    [NotEmptyGuid]
    public Guid? CreatedByUserId { get; set; }
    public bool? Active { get; set; }
}
