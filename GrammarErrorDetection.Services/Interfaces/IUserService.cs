using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Models.Grammar.Users;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserListItemDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<UserListItemDto>> List(PaginationDto<UserList> request);
        Task<UserEntity> Insert(UserInsert request);
        Task<UserEntity> Update(UserUpdate request);
        Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task RequirePasswordChangeAsync(Guid userId);
    }
}
