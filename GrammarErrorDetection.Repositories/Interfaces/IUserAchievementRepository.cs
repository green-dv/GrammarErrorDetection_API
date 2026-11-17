using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IUserAchievementRepository
{
    Task<List<UserAchievementBestItem>> ListBest(Guid userId, PaginationDto<UserAchievementBestList> request);
    /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina.</summary>
    async Task<PagedList<UserAchievementBestItem>> ListBestPage(Guid userId, PaginationDto<UserAchievementBestList> request)
    {
        var items = await ListBest(userId, request);
        return new PagedList<UserAchievementBestItem>(items, items.Count);
    }
}
