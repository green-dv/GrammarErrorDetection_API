using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IAchievementRepository
{
    Task<List<AchievementListItem>> List(PaginationDto<AchievementList> request);
    Task<AchievementEntity> Insert(AchievementInsert request, Guid createdByUserId);
    Task<AchievementEntity> Update(AchievementUpdate request);
}
