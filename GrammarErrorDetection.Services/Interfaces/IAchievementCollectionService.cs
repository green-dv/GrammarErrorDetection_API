using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IAchievementCollectionService
{
    Task<List<AchievementCollectionEntity>> List(PaginationDto<AchievementCollectionList> request);
    async Task<PagedList<AchievementCollectionEntity>> ListPage(PaginationDto<AchievementCollectionList> request)
    {
        var items = await List(request);
        return new PagedList<AchievementCollectionEntity>(items, items.Count);
    }
    Task<AchievementCollectionEntity> Insert(AchievementCollectionInsert request, Guid createdByUserId);
    Task<AchievementCollectionEntity> Update(AchievementCollectionUpdate request);
}
