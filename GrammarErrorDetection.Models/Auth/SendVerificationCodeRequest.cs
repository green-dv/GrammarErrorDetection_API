using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Auth;

public sealed class SendVerificationCodeRequest
{
    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, RegularExpression("^(login|signup)$")]
    public string Purpose { get; set; } = string.Empty;

    // Obligatoria para login; signup solo necesita el correo que se verificará.
    public string? Password { get; set; }
}
