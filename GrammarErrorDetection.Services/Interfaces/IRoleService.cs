using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Roles;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface IRoleService
    {
        Task<List<RoleEntity>> List(PaginationDto<RoleList> request);
        async Task<PagedList<RoleEntity>> ListPage(PaginationDto<RoleList> request)
        {
            var items = await List(request);
            return new PagedList<RoleEntity>(items, items.Count);
        }
        Task<RoleEntity> Insert(RoleInsert request);
        Task<RoleEntity> Update(RoleUpdate request);
    }
}
