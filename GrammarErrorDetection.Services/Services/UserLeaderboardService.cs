using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class UserLeaderboardService(IUserLeaderboardRepository repository) : IUserLeaderboardService
{
    public Task<List<UserLeaderboardItemDto>> List(Guid userId, UserLeaderboardQuery request)
        => repository.List(userId, request);
}
