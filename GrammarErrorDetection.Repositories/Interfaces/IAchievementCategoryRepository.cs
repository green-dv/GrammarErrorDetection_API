using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IAchievementCategoryRepository
{
    Task<List<AchievementCategoryEntity>> List(PaginationDto<AchievementCategoryList> request);
    Task<AchievementCategoryEntity> Insert(AchievementCategoryInsert request);
    Task<AchievementCategoryEntity> Update(AchievementCategoryUpdate request);
}
