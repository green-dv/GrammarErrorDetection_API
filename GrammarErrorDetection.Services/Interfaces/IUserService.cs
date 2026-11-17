using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Models.Grammar.Users;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserListItemDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<UserListItemDto>> List(PaginationDto<UserList> request);
        /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina (las implementaciones reales lo sobrescriben).</summary>
        async Task<PagedList<UserListItemDto>> ListPage(PaginationDto<UserList> request)
        {
            var items = await List(request);
            return new PagedList<UserListItemDto>(items, items.Count);
        }
        Task<UserEntity> Insert(UserInsert request);
        Task<UserEntity> Update(UserUpdate request);
        Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task RequirePasswordChangeAsync(Guid userId);
    }
}
