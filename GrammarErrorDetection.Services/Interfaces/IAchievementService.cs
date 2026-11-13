using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IAchievementService
{
    Task<List<AchievementListItem>> List(PaginationDto<AchievementList> request);
    Task<AchievementEntity> Insert(AchievementInsert request, Guid createdByUserId);
    Task<AchievementEntity> Update(AchievementUpdate request);
    Task<AchievementEntity> InsertWithImage(AchievementInsert request, Guid createdByUserId, Stream? image, CancellationToken cancellationToken = default);
    Task<AchievementEntity> UpdateWithImage(AchievementUpdate request, Stream? image, CancellationToken cancellationToken = default);
}
