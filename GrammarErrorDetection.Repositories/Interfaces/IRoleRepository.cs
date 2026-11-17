using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Roles;

namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IRoleRepository
    {
        Task<List<RoleEntity>> List(PaginationDto<RoleList> request);
        /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina (los repositorios reales lo sobrescriben).</summary>
        async Task<PagedList<RoleEntity>> ListPage(PaginationDto<RoleList> request)
        {
            var items = await List(request);
            return new PagedList<RoleEntity>(items, items.Count);
        }
        Task<RoleEntity> Insert(RoleInsert request);
        Task<RoleEntity> Update(RoleUpdate request);
    }
}

