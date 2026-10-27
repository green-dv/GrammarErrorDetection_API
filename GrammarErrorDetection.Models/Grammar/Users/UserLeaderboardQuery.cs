using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Users;

public sealed class UserLeaderboardQuery
{
    [NotEmptyGuid]
    public Guid UserId { get; set; }
    [Range(1, 6)]
    public int? Bimester { get; set; }
    [Range(1, int.MaxValue)]
    public int? Year { get; set; }
    [NotEmptyGuid]
    public Guid? ModuleNameId { get; set; }
}
