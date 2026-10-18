using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Auth;

public sealed class VerifyMfaRequest
{
    [NotEmptyGuid]
    public Guid ChallengeId { get; set; }

    [Required, RegularExpression("^[0-9]{6}$")]
    public string Code { get; set; } = string.Empty;
}
