using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize]
[Route("api/User/leaderboard")]
public sealed class UserLeaderboardController(IUserLeaderboardService service) : ControllerBase
{
    [HttpPost]
    public async Task<Wrapper<List<UserLeaderboardItemDto>>> List([FromBody] UserLeaderboardQuery request)
    {
        var data = await service.List(request.UserId, request);
        return new() { Data = data, Count = data.Count };
    }
}
