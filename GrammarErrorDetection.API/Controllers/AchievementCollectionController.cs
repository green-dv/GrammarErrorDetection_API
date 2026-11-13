using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize(Roles = "Coordinator,Teacher,Student")]
[Route("api/[controller]")]
public sealed class AchievementCollectionController(IAchievementCollectionService service) : ControllerBase
{
    [HttpGet]
    public async Task<Wrapper<List<AchievementCollectionEntity>>> List([FromQuery] GamificationPage<AchievementCollectionList> request)
    {
        var data = await service.List(request);
        return new() { Data = data, Count = data.Count };
    }

    [HttpPost]
    [Authorize(Roles = "Coordinator")]
    public async Task<Wrapper<AchievementCollectionEntity>> Insert([FromBody] AchievementCollectionInsert request)
    {
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id != Guid.Empty
            ? id : throw new UnauthorizedAccessException();
        var data = await service.Insert(request, userId);
        Response.StatusCode = StatusCodes.Status201Created;
        return new() { Data = data };
    }

    [HttpPut]
    [Authorize(Roles = "Coordinator")]
    public async Task<Wrapper<AchievementCollectionEntity>> Update([FromBody] AchievementCollectionUpdate request)
        => new() { Data = await service.Update(request) };
}
