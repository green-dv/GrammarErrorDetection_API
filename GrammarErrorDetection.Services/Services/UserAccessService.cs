using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Roles;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

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
        RequireActor(actor);
        ArgumentNullException.ThrowIfNull(request);
        if (actor.IsCoordinator) return await users.List(request);
        if (!actor.IsTeacher) throw new AccessDeniedException("No tienes permiso para buscar usuarios.");
        if (request.Filter?.ClassId is not { } classId || classId == Guid.Empty)
            throw new AccessDeniedException("Un docente solo puede buscar estudiantes de uno de sus cursos.");
        if (!(await TeacherClassIdsAsync(actor.Id)).Contains(classId))
            throw new AccessDeniedException("El curso no pertenece al docente.");
        return await users.List(request);
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
