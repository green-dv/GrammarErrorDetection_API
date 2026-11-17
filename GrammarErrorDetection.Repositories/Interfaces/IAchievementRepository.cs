using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IAchievementRepository
{
    Task<List<AchievementListItem>> List(PaginationDto<AchievementList> request);
    /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina (los repositorios reales lo sobrescriben).</summary>
    async Task<PagedList<AchievementListItem>> ListPage(PaginationDto<AchievementList> request)
    {
        var items = await List(request);
        return new PagedList<AchievementListItem>(items, items.Count);
    }
    Task<AchievementEntity> Insert(AchievementInsert request, Guid createdByUserId);
    Task<AchievementEntity> Update(AchievementUpdate request);
}
