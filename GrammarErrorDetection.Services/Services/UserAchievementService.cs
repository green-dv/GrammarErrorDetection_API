using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class UserAchievementService(IUserAchievementRepository repository) : IUserAchievementService
{
    public Task<List<UserAchievementBestItem>> ListBest(Guid userId, PaginationDto<UserAchievementBestList> request)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId es obligatorio y debe ser un UUID distinto de vacío.", nameof(userId));
        return repository.ListBest(userId, request);
    }
}
