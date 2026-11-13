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
    Task<UserEntity> UpdateAsync(UserActor actor, UserUpdate request);
}
