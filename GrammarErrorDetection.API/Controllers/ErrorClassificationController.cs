using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.ErrorClassifications;
using GrammarErrorDetection.Models.Grammar.Validation;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ErrorClassificationController(IErrorClassificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<Wrapper<List<ErrorClassificationEntity>>> List([FromQuery] PaginationDto<ErrorClassificationList> request)
    {
        var data = await service.List(request);
        return new() { Data = data, Count = data.Count };
    }

    [HttpGet("{id:guid}")]
    public async Task<Wrapper<ErrorClassificationEntity>> Get([FromRoute, NotEmptyGuid] Guid id)
        => new() { Data = await service.Get(id) };
}
