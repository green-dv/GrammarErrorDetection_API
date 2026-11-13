using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Auth;

/// <summary>Cambio obligatorio de contraseña durante el inicio de sesión, antes de recibir el JWT.</summary>
public sealed class CompletePasswordChangeRequest
{
    [NotEmptyGuid]
    public Guid ChallengeId { get; set; }

    [Required]
    [StringLength(200)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(72, MinimumLength = 8)]
    [StrongPassword]
    public string NewPassword { get; set; } = string.Empty;
}
