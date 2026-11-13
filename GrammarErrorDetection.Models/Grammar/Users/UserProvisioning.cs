using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Users;

/// <summary>Alta de un usuario por el coordinador: sin contraseña (se genera una temporal y se envía por correo).</summary>
public sealed class UserCreateRequest
{
    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(15)]
    public string CI { get; set; } = string.Empty;

    [Required, StringLength(100), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(12), Phone]
    public string? Phone { get; set; }

    [Required, NotEmptyGuid]
    public Guid? RoleId { get; set; }

    /// <summary>Opcional. No puede ser futura.</summary>
    public DateOnly? FechaNacimiento { get; set; }
}

/// <summary>Resultado del alta o del reenvío. La contraseña temporal nunca viaja en la respuesta.</summary>
public sealed class UserCredentialsResult
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool CredentialsSent { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class UserProvisioningOptions
{
    /// <summary>Enlace de inicio de sesión incluido en el correo; opcional.</summary>
    public string? LoginUrl { get; set; }
}
