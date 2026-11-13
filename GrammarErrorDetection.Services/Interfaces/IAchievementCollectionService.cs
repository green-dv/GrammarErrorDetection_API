using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IAchievementCollectionService
{
    Task<List<AchievementCollectionEntity>> List(PaginationDto<AchievementCollectionList> request);
    Task<AchievementCollectionEntity> Insert(AchievementCollectionInsert request, Guid createdByUserId);
    Task<AchievementCollectionEntity> Update(AchievementCollectionUpdate request);
}
