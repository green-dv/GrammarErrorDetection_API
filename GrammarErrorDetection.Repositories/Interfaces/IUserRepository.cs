using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Users;

namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<List<UserEntity>> List(PaginationDto<UserList> request);
        /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina (las implementaciones reales lo sobrescriben).</summary>
        async Task<PagedList<UserEntity>> ListPage(PaginationDto<UserList> request)
        {
            var items = await List(request);
            return new PagedList<UserEntity>(items, items.Count);
        }
        Task<UserEntity> Insert(UserInsert request);
        Task<UserEntity> Update(UserUpdate request);
    }
}

