using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Models.Grammar.Validation;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class UserAchievementController(IUserAchievementService service) : ControllerBase
{
    [HttpGet("best")]
    public async Task<Wrapper<List<UserAchievementBestItem>>> ListBest(
        [FromQuery, BindRequired, NotEmptyGuid] Guid userId,
        [FromQuery] GamificationPage<UserAchievementBestList> request)
    {
        var data = await service.ListBest(userId, request);
        return new() { Data = data, Count = data.Count };
    }
}
