using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IUserAchievementService
{
    Task<List<UserAchievementBestItem>> ListBest(Guid userId, PaginationDto<UserAchievementBestList> request);
}
