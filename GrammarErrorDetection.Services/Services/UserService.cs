using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services
{
    public sealed class UserService : IUserService
    {
        private readonly IUserRepository repository;

        public UserService(IUserRepository repository)
        {
            ArgumentNullException.ThrowIfNull(repository);
            this.repository = repository;
        }

        public Task<List<UserEntity>> List(PaginationDto<UserList> request)
            => repository.List(request);

        public Task<UserEntity> Insert(UserInsert request)
            => repository.Insert(request);

        public Task<UserEntity> Update(UserUpdate request)
            => repository.Update(request);
    }
}
