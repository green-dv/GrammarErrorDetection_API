using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IAchievementCollectionRepository
{
    Task<List<AchievementCollectionEntity>> List(PaginationDto<AchievementCollectionList> request);
    /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina (los repositorios reales lo sobrescriben).</summary>
    async Task<PagedList<AchievementCollectionEntity>> ListPage(PaginationDto<AchievementCollectionList> request)
    {
        var items = await List(request);
        return new PagedList<AchievementCollectionEntity>(items, items.Count);
    }
    Task<AchievementCollectionEntity> Insert(AchievementCollectionInsert request, Guid createdByUserId);
    Task<AchievementCollectionEntity> Update(AchievementCollectionUpdate request);
}
