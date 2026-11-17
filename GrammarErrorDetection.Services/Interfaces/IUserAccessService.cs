using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Users;

namespace GrammarErrorDetection.Services.Interfaces;

/// <summary>Usuario autenticado según el JWT.</summary>
public sealed record UserActor(Guid Id, IReadOnlyCollection<string> Roles)
{
    public bool IsCoordinator => Roles.Contains("Coordinator");
    public bool IsTeacher => Roles.Contains("Teacher");
}

/// <summary>Lectura y edición de usuarios con el alcance de cada rol.</summary>
public interface IUserAccessService
{
    Task<UserListItemDto> GetAsync(UserActor actor, Guid userId, CancellationToken cancellationToken = default);
    Task<List<UserListItemDto>> SearchAsync(UserActor actor, PaginationDto<UserList> request);
    /// <summary>Página y total sin paginar; por defecto el total es el de la página.</summary>
    async Task<PagedList<UserListItemDto>> SearchPageAsync(UserActor actor, PaginationDto<UserList> request)
    {
        var items = await SearchAsync(actor, request);
        return new PagedList<UserListItemDto>(items, items.Count);
    }
    Task<UserEntity> UpdateAsync(UserActor actor, UserUpdate request);
}
