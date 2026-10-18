using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Roles;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services
{
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

        public Task<RoleEntity> Insert(RoleInsert request)
            => repository.Insert(request);

        public Task<RoleEntity> Update(RoleUpdate request)
            => repository.Update(request);
    }
}
