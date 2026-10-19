using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Evaluations;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize(Roles = "Coordinator")]
[Route("api/[controller]")]
public sealed class EvaluationController : ControllerBase
{
    private readonly IEvaluationService service;

    public EvaluationController(IEvaluationService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        this.service = service;
    }

    [HttpGet]
    public async Task<Wrapper<List<EvaluationEntity>>> List([FromQuery] PaginationDto<EvaluationList> request)
    {
        var data = await service.List(request);
        return new Wrapper<List<EvaluationEntity>> { Data = data, Count = data.Count };
    }

    [HttpPost]
    public async Task<Wrapper<EvaluationEntity>> Insert([FromBody] EvaluationInsert request)
    {
        var data = await service.Insert(request);
        Response.StatusCode = StatusCodes.Status201Created;
        return new Wrapper<EvaluationEntity> { Data = data };
    }

    [HttpPut]
    public async Task<Wrapper<EvaluationEntity>> Update([FromBody] EvaluationUpdate request)
    {
        var data = await service.Update(request);
        return new Wrapper<EvaluationEntity> { Data = data };
    }
}
