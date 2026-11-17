using GrammarErrorDetection.Models.Grammar.Users;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IUserLeaderboardService
{
    Task<List<UserLeaderboardItemDto>> List(Guid userId, UserLeaderboardQuery request);
}
