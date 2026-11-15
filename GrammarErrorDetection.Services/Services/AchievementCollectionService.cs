using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class AchievementCollectionService(IAchievementCollectionRepository repository) : IAchievementCollectionService
{
    public Task<List<AchievementCollectionEntity>> List(PaginationDto<AchievementCollectionList> request) => repository.List(request);
    public Task<AchievementCollectionEntity> Insert(AchievementCollectionInsert request, Guid createdByUserId) => repository.Insert(request, createdByUserId);
    public Task<AchievementCollectionEntity> Update(AchievementCollectionUpdate request) => repository.Update(request);
}
