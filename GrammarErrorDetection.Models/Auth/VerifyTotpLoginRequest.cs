using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Auth;

public sealed class VerifyTotpLoginRequest : LoginRequest
{
    [Required, RegularExpression("^[0-9]{6}$")]
    public string Code { get; set; } = string.Empty;
}
