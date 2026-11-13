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
public sealed class ConditionTypeController(IConditionTypeService service) : ControllerBase
{
    [HttpGet]
    public async Task<Wrapper<List<ConditionTypeEntity>>> List([FromQuery] GamificationPage<ConditionTypeList> request)
    {
        var data = await service.List(request);
        return new() { Data = data, Count = data.Count };
    }

    [HttpPost]
    public async Task<Wrapper<ConditionTypeEntity>> Insert([FromBody] ConditionTypeInsert request)
    {
        var data = await service.Insert(request);
        Response.StatusCode = StatusCodes.Status201Created;
        return new() { Data = data };
    }

    [HttpPut]
    public async Task<Wrapper<ConditionTypeEntity>> Update([FromBody] ConditionTypeUpdate request)
        => new() { Data = await service.Update(request) };
}
