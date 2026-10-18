using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Auth;

public sealed class TotpCodeRequest
{
    [Required, RegularExpression("^[0-9]{6}$")]
    public string Code { get; set; } = string.Empty;
}

public sealed class TotpSetupResponse
{
    public string OtpAuthUri { get; set; } = string.Empty;
}
