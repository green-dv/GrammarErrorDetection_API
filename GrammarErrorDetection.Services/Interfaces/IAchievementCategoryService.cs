using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IAchievementCategoryService
{
    Task<List<AchievementCategoryEntity>> List(PaginationDto<AchievementCategoryList> request);
    async Task<PagedList<AchievementCategoryEntity>> ListPage(PaginationDto<AchievementCategoryList> request)
    {
        var items = await List(request);
        return new PagedList<AchievementCategoryEntity>(items, items.Count);
    }
    Task<AchievementCategoryEntity> Insert(AchievementCategoryInsert request);
    Task<AchievementCategoryEntity> Update(AchievementCategoryUpdate request);
}
