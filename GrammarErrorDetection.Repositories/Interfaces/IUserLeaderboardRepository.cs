using GrammarErrorDetection.Models.Grammar.Users;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IUserLeaderboardRepository
{
    Task<List<UserLeaderboardItemDto>> List(Guid userId, UserLeaderboardQuery request);
}
