using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class AchievementCategoryService(IAchievementCategoryRepository repository) : IAchievementCategoryService
{
    public Task<List<AchievementCategoryEntity>> List(PaginationDto<AchievementCategoryList> request) => repository.List(request);
    public Task<AchievementCategoryEntity> Insert(AchievementCategoryInsert request) => repository.Insert(request);
    public Task<AchievementCategoryEntity> Update(AchievementCategoryUpdate request) => repository.Update(request);
}
