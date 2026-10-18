using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Roles;

namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IRoleRepository
    {
        Task<List<RoleEntity>> List(PaginationDto<RoleList> request);
        Task<RoleEntity> Insert(RoleInsert request);
        Task<RoleEntity> Update(RoleUpdate request);
    }
}

