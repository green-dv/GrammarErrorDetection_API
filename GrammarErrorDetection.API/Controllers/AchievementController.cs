using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize(Roles = "Coordinator,Teacher,Student")]
[Route("api/[controller]")]
public sealed class AchievementController(IAchievementService service) : ControllerBase
{
    [HttpGet]
    public async Task<Wrapper<List<AchievementListItem>>> List([FromQuery] GamificationPage<AchievementList> request)
    {
        var data = await service.List(request);
        return new() { Data = data, Count = data.Count };
    }

    [HttpPost]
    [Authorize(Roles = "Coordinator")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(AchievementFileOptions.MaxBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = AchievementFileOptions.MaxBytes + 64 * 1024)]
    public async Task<Wrapper<AchievementEntity>> InsertWithImage([FromForm] AchievementFormRequest request, CancellationToken cancellationToken)
    {
        var input = request.Parse<AchievementInsert>();
        await using var image = request.File?.OpenReadStream();
        var data = await service.InsertWithImage(input, UserId(), image, cancellationToken);
        Response.StatusCode = StatusCodes.Status201Created;
        return new() { Data = data };
    }

    [HttpPut]
    [Authorize(Roles = "Coordinator")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(AchievementFileOptions.MaxBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = AchievementFileOptions.MaxBytes + 64 * 1024)]
    public async Task<Wrapper<AchievementEntity>> UpdateWithImage([FromForm] AchievementFormRequest request, CancellationToken cancellationToken)
    {
        var input = request.Parse<AchievementUpdate>();
        await using var image = request.File?.OpenReadStream();
        return new() { Data = await service.UpdateWithImage(input, image, cancellationToken) };
    }

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id != Guid.Empty
        ? id : throw new UnauthorizedAccessException();
}
