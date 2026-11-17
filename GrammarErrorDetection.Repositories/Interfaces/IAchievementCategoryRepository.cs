using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IAchievementCategoryRepository
{
    Task<List<AchievementCategoryEntity>> List(PaginationDto<AchievementCategoryList> request);
    /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina (los repositorios reales lo sobrescriben).</summary>
    async Task<PagedList<AchievementCategoryEntity>> ListPage(PaginationDto<AchievementCategoryList> request)
    {
        var items = await List(request);
        return new PagedList<AchievementCategoryEntity>(items, items.Count);
    }
    Task<AchievementCategoryEntity> Insert(AchievementCategoryInsert request);
    Task<AchievementCategoryEntity> Update(AchievementCategoryUpdate request);
}
