using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IUserAchievementRepository
{
    Task<List<UserAchievementBestItem>> ListBest(Guid userId, PaginationDto<UserAchievementBestList> request);
}
