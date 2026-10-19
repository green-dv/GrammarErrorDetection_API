using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Users;

namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<List<UserEntity>> List(PaginationDto<UserList> request);
        Task<List<UserSummaryDto>> ListSummary(PaginationDto<UserSummaryList> request);
        Task<UserEntity> Insert(UserInsert request);
        Task<UserEntity> Update(UserUpdate request);
    }
}

