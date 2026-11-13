using GrammarErrorDetection.Models.Grammar.Users;

namespace GrammarErrorDetection.Services.Interfaces;

/// <summary>Alta de usuarios por el coordinador con contraseña temporal enviada por correo.</summary>
public interface IUserProvisioningService
{
    Task<UserCredentialsResult> CreateAsync(UserCreateRequest request);
    /// <summary>Genera otra contraseña temporal, exige cambiarla y la reenvía. No aplica a la propia cuenta.</summary>
    Task<UserCredentialsResult> ResendCredentialsAsync(Guid coordinatorId, Guid userId);
}
