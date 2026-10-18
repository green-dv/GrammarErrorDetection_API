using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Models.Grammar.Users;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<UserEntity>> List(PaginationDto<UserList> request);
        Task<UserEntity> Insert(UserInsert request);
        Task<UserEntity> Update(UserUpdate request);
        Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task RequirePasswordChangeAsync(Guid userId);
    }
}
