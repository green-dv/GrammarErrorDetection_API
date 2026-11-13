using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize(Roles = "Coordinator")]
[Route("api/[controller]")]
public sealed class AchievementCategoryController(IAchievementCategoryService service) : ControllerBase
{
    [HttpGet]
    public async Task<Wrapper<List<AchievementCategoryEntity>>> List([FromQuery] GamificationPage<AchievementCategoryList> request)
    {
        var data = await service.List(request);
        return new() { Data = data, Count = data.Count };
    }

    [HttpPost]
    public async Task<Wrapper<AchievementCategoryEntity>> Insert([FromBody] AchievementCategoryInsert request)
    {
        var data = await service.Insert(request);
        Response.StatusCode = StatusCodes.Status201Created;
        return new() { Data = data };
    }

    [HttpPut]
    public async Task<Wrapper<AchievementCategoryEntity>> Update([FromBody] AchievementCategoryUpdate request)
        => new() { Data = await service.Update(request) };
}
