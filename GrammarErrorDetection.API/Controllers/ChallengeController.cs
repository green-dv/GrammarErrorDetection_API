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
public sealed class ChallengeController(IChallengeService service) : ControllerBase
{
    [HttpGet]
    public async Task<Wrapper<List<ChallengeListItem>>> List([FromQuery] GamificationPage<ChallengeList> request)
    {
        var data = await service.List(request);
        return new() { Data = data, Count = data.Count };
    }

    [HttpPost]
    [Authorize(Roles = "Coordinator")]
    public async Task<Wrapper<ChallengeEntity>> Insert([FromBody] ChallengeInsert request)
    {
        var data = await service.Insert(request, UserId());
        Response.StatusCode = StatusCodes.Status201Created;
        return new() { Data = data };
    }

    [HttpPut]
    [Authorize(Roles = "Coordinator")]
    public async Task<Wrapper<ChallengeEntity>> Update([FromBody] ChallengeUpdate request)
        => new() { Data = await service.Update(request) };

    /// <summary>Repetir: abre una nueva ronda con nuevas fechas y vuelve a habilitar el desafío para todos.</summary>
    [HttpPost("{id:guid}/repeat")]
    [Authorize(Roles = "Coordinator")]
    public async Task<Wrapper<ChallengeEntity>> Repeat([FromRoute] Guid id, [FromBody] ChallengeRepeat request)
        => new() { Data = await service.Repeat(id, request) };

    /// <summary>Duplicar: crea una copia en borrador con las mismas condiciones.</summary>
    [HttpPost("{id:guid}/duplicate")]
    [Authorize(Roles = "Coordinator")]
    public async Task<Wrapper<ChallengeEntity>> Duplicate([FromRoute] Guid id)
    {
        var data = await service.Duplicate(id, UserId());
        Response.StatusCode = StatusCodes.Status201Created;
        return new() { Data = data };
    }

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id != Guid.Empty
        ? id : throw new UnauthorizedAccessException();
}
