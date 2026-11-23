using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Roles;
using GrammarErrorDetection.Services.Exceptions;
using System.Security.Cryptography;

namespace GrammarErrorDetection.Services.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository repository;

    public UserService(IUserRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        this.repository = repository;
    }

    public async Task<List<UserListItemDto>> List(PaginationDto<UserList> request)
        => (await repository.List(request)).Select(ToListItem).ToList();

    public async Task<PagedList<UserListItemDto>> ListPage(PaginationDto<UserList> request)
    {
        var page = await repository.ListPage(request);
        return new PagedList<UserListItemDto>(page.Items.Select(ToListItem).ToList(), page.TotalCount);
    }

    private static UserListItemDto ToListItem(UserEntity user)
        => new UserListItemDto
        {
            Id = user.Id, FirstName = user.FirstName, LastName = user.LastName,
            CI = user.CI, Email = user.Email, Phone = user.Phone, FechaNacimiento = user.FechaNacimiento,
            RoleId = user.RoleId, RoleName = user.RoleName,
            HasAuthenticator = user.HasAuthenticator, IsTotpEnabled = user.IsTotpEnabled,
            ChangePassword = user.ChangePassword, CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt, Active = user.Active,
            CurrentCourse = user.CurrentCourse, CurrentCourseFinalScore = user.CurrentCourseFinalScore,
            LastCourse = user.LastCourse
        };

    public async Task<UserListItemDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("El identificador de usuario no puede estar vacío.", nameof(userId));
        // User_List no admite Id: la resolución se realiza en el servidor sin alterar su contrato.
        var seen = new HashSet<Guid>();
        for (var page = 1; ; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var users = await List(new PaginationDto<UserList> { PageNumber = page, PageSize = 100 });
            cancellationToken.ThrowIfCancellationRequested();
            var found = users.SingleOrDefault(user => user.Id == userId);
            if (found is not null) return found;
            if (users.Count < 100) throw new KeyNotFoundException("No se encontró el usuario solicitado.");
            if (users.Count(user => seen.Add(user.Id)) == 0)
                throw new InvalidOperationException("La consulta de usuarios no avanzó de página.");
        }
    }
    public Task<UserEntity> Insert(UserInsert request)
        => repository.Insert(request);

    public Task<UserEntity> Update(UserUpdate request)
        => repository.Update(request);

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        if (Encoding.UTF8.GetByteCount(request.Password) > 72
            || Encoding.UTF8.GetByteCount(request.NewPassword) > 72)
            throw new ValidationException("La contraseña no puede superar 72 bytes UTF-8 para BCrypt.");

        var user = await FindActiveUserAsync(userId);
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            throw new ValidationException("La contraseña actual es incorrecta.");
        if (string.Equals(request.Password, request.NewPassword, StringComparison.Ordinal))
            throw new ValidationException("La nueva contraseña debe ser distinta de la actual.");

        await repository.Update(new UserUpdate
        {
            Id = userId,
            Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword),
            ChangePassword = false
        });
    }

    public async Task RequirePasswordChangeAsync(Guid userId)
    {
        await FindActiveUserAsync(userId);
        await repository.Update(new UserUpdate { Id = userId, ChangePassword = true });
    }

    private async Task<UserEntity> FindActiveUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ValidationException("El identificador de usuario no puede estar vacío.");

        // User_List no recibe Id; se recorren sus páginas sin cambiar el contrato del SP.
        for (var page = 1; ; page++)
        {
            var users = await repository.List(new PaginationDto<UserList>
            {
                PageNumber = page, PageSize = 100, Filter = new UserList { Active = true }
            });
            var user = users.SingleOrDefault(user => user.Id == userId && user.Active);
            if (user is not null)
                return user;
            if (users.Count < 100)
                throw new KeyNotFoundException("No se encontró el usuario activo.");
        }
    }
}

/// <summary>
/// Reglas de acceso a usuarios. El actor sale siempre del JWT (nunca del cuerpo de la petición).
/// <list type="bullet">
/// <item>Edición: cada usuario solo edita sus datos personales (nombre, apellido, teléfono); el coordinador edita a cualquiera,
/// incluido rol y estado. La contraseña y el secreto TOTP nunca se cambian por este endpoint.</item>
/// <item>Nunca queda el sistema sin un coordinador activo.</item>
/// <item>Lectura: el coordinador ve todo; el docente busca solo alumnos de sus cursos; los datos de contacto
/// (CI, correo, teléfono) de otra persona solo los ven el coordinador y el docente de esa persona.</item>
/// </list>
/// </summary>
public sealed class UserAccessService(IUserService users, IUserRepository repository, IRoleRepository roles, IClassRepository classes)
    : IUserAccessService
{
    public async Task<UserListItemDto> GetAsync(UserActor actor, Guid userId, CancellationToken cancellationToken = default)
    {
        RequireActor(actor);
        var user = await users.GetAsync(userId, cancellationToken);
        if (actor.IsCoordinator || actor.Id == userId) return user;
        if (actor.IsTeacher && await TeachesStudentAsync(actor.Id, user)) return user;
        // Perfil público: sin datos de contacto ni identificación.
        user.CI = string.Empty;
        user.Email = string.Empty;
        user.Phone = null;
        user.FechaNacimiento = null;
        return user;
    }

    public async Task<List<UserListItemDto>> SearchAsync(UserActor actor, PaginationDto<UserList> request)
    {
        await AuthorizeSearchAsync(actor, request);
        return await users.List(request);
    }

    public async Task<PagedList<UserListItemDto>> SearchPageAsync(UserActor actor, PaginationDto<UserList> request)
    {
        await AuthorizeSearchAsync(actor, request);
        return await users.ListPage(request);
    }

    private async Task AuthorizeSearchAsync(UserActor actor, PaginationDto<UserList> request)
    {
        RequireActor(actor);
        ArgumentNullException.ThrowIfNull(request);
        if (actor.IsCoordinator) return;
        if (!actor.IsTeacher) throw new AccessDeniedException("No tienes permiso para buscar usuarios.");
        if (request.Filter?.ClassId is not { } classId || classId == Guid.Empty)
            throw new AccessDeniedException("Un docente solo puede buscar estudiantes de uno de sus cursos.");
        if (!(await TeacherClassIdsAsync(actor.Id)).Contains(classId))
            throw new AccessDeniedException("El curso no pertenece al docente.");
    }

    public async Task<UserEntity> UpdateAsync(UserActor actor, UserUpdate request)
    {
        RequireActor(actor);
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id == Guid.Empty) throw new ValidationException("Indica el usuario a editar.");
        if (request.Password is not null || request.TotpSecret is not null)
            throw new AccessDeniedException("La contraseña y el autenticador no se modifican desde este endpoint.");
        RequireValidBirthDate(request.FechaNacimiento);

        if (!actor.IsCoordinator)
        {
            if (request.Id != actor.Id) throw new AccessDeniedException("Solo puedes editar tu propio perfil.");
            if (request.RoleId is not null || request.Active is not null || request.CI is not null || request.Email is not null
                || request.ChangePassword is not null || request.HasAuthenticator is not null || request.IsTotpEnabled is not null)
                throw new AccessDeniedException("Solo puedes modificar tu nombre, apellido, teléfono y fecha de nacimiento.");
            return await repository.Update(request);
        }

        // El coordinador puede desactivar el autenticador de otra persona, pero no activarlo por ella.
        if (request.HasAuthenticator is true || request.IsTotpEnabled is true)
            throw new AccessDeniedException("El autenticador solo lo activa su propietario.");
        if (request.RoleId == Guid.Empty) throw new ValidationException("El rol no es válido.");
        if (request.Active is false || request.RoleId is not null)
            await KeepOneCoordinatorAsync(request);
        return await repository.Update(request);
    }

    /// <summary>Fecha de nacimiento opcional: no futura ni anterior a 1900.</summary>
    public static DateOnly? RequireValidBirthDate(DateOnly? value)
    {
        if (value is not { } date) return null;
        if (date > DateOnly.FromDateTime(DateTime.Today)) throw new ValidationException("La fecha de nacimiento no puede ser futura.");
        if (date.Year < 1900) throw new ValidationException("La fecha de nacimiento no es válida.");
        return date;
    }

    private async Task KeepOneCoordinatorAsync(UserUpdate request)
    {
        var coordinatorRole = (await roles.List(new PaginationDto<RoleList> { PageNumber = 1, PageSize = 100, Filter = new() { Name = "Coordinator" } }))
            .FirstOrDefault(r => string.Equals(r.Name, "Coordinator", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("No existe el rol de coordinador.");
        var target = await users.GetAsync(request.Id);
        var losesRole = target.RoleId == coordinatorRole.Id && target.Active
            && (request.Active is false || (request.RoleId is { } role && role != coordinatorRole.Id));
        if (!losesRole) return;
        var others = 0;
        for (var page = 1; others == 0; page++)
        {
            var batch = await repository.List(new PaginationDto<UserList>
            {
                PageNumber = page, PageSize = 100, Filter = new UserList { RoleId = coordinatorRole.Id, Active = true }
            });
            others += batch.Count(u => u.Id != request.Id && u.Active && u.RoleId == coordinatorRole.Id);
            if (batch.Count < 100) break;
        }
        if (others == 0)
            throw new ValidationException("Debe quedar al menos un coordinador activo. Asigna otro coordinador antes de quitar este.");
    }

    private async Task<bool> TeachesStudentAsync(Guid teacherId, UserListItemDto user)
    {
        var courses = new[] { user.CurrentCourse?.ClassId, user.LastCourse?.ClassId }.OfType<Guid>().ToList();
        if (courses.Count == 0) return false;
        var mine = await TeacherClassIdsAsync(teacherId);
        return courses.Any(mine.Contains);
    }

    private async Task<HashSet<Guid>> TeacherClassIdsAsync(Guid teacherId)
    {
        var ids = new HashSet<Guid>();
        for (var page = 1; ; page++)
        {
            var batch = await classes.List(new PaginationDto<ClassList> { PageNumber = page, PageSize = 100, Filter = new() { TeacherId = teacherId } });
            var added = batch.Count(c => c.TeacherId == teacherId && ids.Add(c.Id));
            if (batch.Count < 100 || added == 0) return ids;
        }
    }

    private static void RequireActor(UserActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (actor.Id == Guid.Empty) throw new UnauthorizedAccessException("El token no identifica al usuario.");
    }
}

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
        var message = (created ? $"se creó tu cuenta en el portal de Grammar Practice con el rol {RoleLabel(roleName)}." : "se generaron nuevas credenciales para tu cuenta del portal de Grammar Practice.")
            + $"\n\nUsuario: {user.Email}\nContraseña temporal: {password}\n{link}"
            + "\nPor seguridad, al iniciar sesión se te pedirá crear una nueva contraseña. No compartas este correo.";
        try
        {
            await email.SendVerificationEmailAsync(fullName, user.Email, message, created ? "Tu cuenta de Grammar Practice" : "Nuevas credenciales de Grammar Practice");
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

public sealed class RoleService : IRoleService
{
    private readonly IRoleRepository repository;

    public RoleService(IRoleRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        this.repository = repository;
    }

    public Task<List<RoleEntity>> List(PaginationDto<RoleList> request)
        => repository.List(request);

    public Task<PagedList<RoleEntity>> ListPage(PaginationDto<RoleList> request) => repository.ListPage(request);

    public Task<RoleEntity> Insert(RoleInsert request)
        => repository.Insert(request);

    public Task<RoleEntity> Update(RoleUpdate request)
        => repository.Update(request);
}
