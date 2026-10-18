using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Auth;

public sealed class ChangePasswordRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(72, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}
