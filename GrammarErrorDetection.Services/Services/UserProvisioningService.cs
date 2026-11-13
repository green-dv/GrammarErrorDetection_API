using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Roles;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

/// <summary>
/// El coordinador crea cuentas con un rol definido. Se genera una contraseña temporal aleatoria, se guarda solo su hash,
/// se marca ChangePassword y se envía al correo del nuevo usuario. El login exige cambiarla antes de entregar el JWT.
/// </summary>
public sealed class UserProvisioningService(IUserRepository users, IRoleRepository roles, IEmailRepository email,
    UserProvisioningOptions options) : IUserProvisioningService
{
    // Sin caracteres ambiguos (0/O, 1/l/I) para que se pueda copiar a mano desde el correo.
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%*?";
    internal const int PasswordLength = 12;

    public async Task<UserCredentialsResult> CreateAsync(UserCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        var emailAddress = request.Email.Trim();
        var ci = request.CI.Trim();
        var role = await ActiveRoleAsync(request.RoleId!.Value);
        if (await FindAsync(new UserList { Email = emailAddress }, u => Same(u.Email, emailAddress)) is not null
            || await FindAsync(new UserList { CI = ci }, u => u.CI == ci) is not null)
            throw new AuthConflictException("El correo o la cédula de identidad ya están registrados.");

        var password = GeneratePassword();
        var user = await users.Insert(new UserInsert
        {
            FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), CI = ci, Email = emailAddress,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Password = BCrypt.Net.BCrypt.HashPassword(password), RoleId = role.Id, ChangePassword = true, Active = true,
            FechaNacimiento = UserAccessService.RequireValidBirthDate(request.FechaNacimiento)
        });
        return await SendAsync(user, role.Name, password, created: true);
    }

    public async Task<UserCredentialsResult> ResendCredentialsAsync(Guid coordinatorId, Guid userId)
    {
        if (userId == Guid.Empty) throw new ValidationException("Indica el usuario.");
        if (userId == coordinatorId) throw new AccessDeniedException("No puedes reenviarte credenciales a ti mismo; usa Cambiar contraseña.");
        var user = await FindAsync(new UserList { Active = true }, u => u.Id == userId && u.Active)
            ?? throw new KeyNotFoundException("No se encontró un usuario activo con ese identificador.");
        var password = GeneratePassword();
        await users.Update(new UserUpdate { Id = user.Id, Password = BCrypt.Net.BCrypt.HashPassword(password), ChangePassword = true });
        return await SendAsync(user, user.RoleName, password, created: false);
    }

    /// <summary>12 caracteres con al menos una mayúscula, minúscula, dígito y símbolo, mezclados al azar.</summary>
    internal static string GeneratePassword()
    {
        var all = Upper + Lower + Digits + Symbols;
        var chars = new List<char> { Pick(Upper), Pick(Lower), Pick(Digits), Pick(Symbols) };
        while (chars.Count < PasswordLength) chars.Add(Pick(all));
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars.ToArray());
    }

    private static char Pick(string source) => source[RandomNumberGenerator.GetInt32(source.Length)];

    private async Task<UserCredentialsResult> SendAsync(UserEntity user, string roleName, string password, bool created)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var result = new UserCredentialsResult { UserId = user.Id, Email = user.Email, FullName = fullName, RoleName = roleName };
        var link = string.IsNullOrWhiteSpace(options.LoginUrl) ? "" : $"\nIngresa en: {options.LoginUrl}\n";
        var message = (created ? $"se creó tu cuenta en el portal del CETI con el rol {RoleLabel(roleName)}." : "se generaron nuevas credenciales para tu cuenta del portal del CETI.")
            + $"\n\nUsuario: {user.Email}\nContraseña temporal: {password}\n{link}"
            + "\nPor seguridad, al iniciar sesión se te pedirá crear una nueva contraseña. No compartas este correo.";
        try
        {
            await email.SendVerificationEmailAsync(fullName, user.Email, message, created ? "Tu cuenta del CETI" : "Nuevas credenciales del CETI");
            result.CredentialsSent = true;
            result.Message = created ? "Usuario creado. Se enviaron las credenciales a su correo." : "Se enviaron nuevas credenciales a su correo.";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // La cuenta queda con una contraseña que nadie conoce: el coordinador debe usar "Reenviar credenciales".
            result.Message = created
                ? "Usuario creado, pero no se pudo enviar el correo. Usa «Reenviar credenciales»."
                : "No se pudo enviar el correo. Inténtalo de nuevo.";
        }
        return result;
    }

    private static string RoleLabel(string role) => role switch
    {
        "Teacher" => "Docente",
        "Coordinator" => "Coordinador",
        "Student" => "Estudiante",
        _ => role
    };

    private async Task<RoleEntity> ActiveRoleAsync(Guid roleId)
    {
        var role = (await roles.List(new PaginationDto<RoleList> { PageNumber = 1, PageSize = 1, Filter = new() { Id = roleId, Active = true } }))
            .SingleOrDefault(r => r.Id == roleId && r.Active);
        return role ?? throw new ValidationException("El rol no existe o está inactivo.");
    }

    private async Task<UserEntity?> FindAsync(UserList filter, Func<UserEntity, bool> matches)
    {
        // User_List filtra con ILIKE parcial: la coincidencia exacta puede estar en otra página.
        for (var page = 1; ; page++)
        {
            var batch = await users.List(new PaginationDto<UserList> { PageNumber = page, PageSize = 100, Filter = filter });
            if (batch.FirstOrDefault(matches) is { } found) return found;
            if (batch.Count < 100) return null;
        }
    }

    private static bool Same(string? left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
