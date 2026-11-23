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
        var page = await service.List(request);
        return new() { Data = page.Items, Count = page.Items.Count, TotalCount = page.TotalCount };
    }

    [HttpGet("{id:guid}")]
    public async Task<Wrapper<ErrorClassificationEntity>> Get([FromRoute, NotEmptyGuid] Guid id)
        => new() { Data = await service.Get(id) };

    // La lectura queda abierta a cualquier usuario autenticado (filtros de materiales y reportes);
    // solo el coordinador crea, edita o deshabilita categorías.
    [HttpPost]
    [Authorize(Roles = "Teacher,Coordinator")]
    public async Task<Wrapper<ErrorClassificationEntity>> Insert([FromBody] ErrorClassificationInsert request)
    {
        var data = await service.Insert(request);
        Response.StatusCode = StatusCodes.Status201Created;
        return new() { Data = data };
    }

    /// <summary>Edita la categoría; Active = false la deshabilita y Active = true la reactiva.</summary>
    [HttpPut]
    [Authorize(Roles = "Teacher,Coordinator")]
    public async Task<Wrapper<ErrorClassificationEntity>> Update([FromBody] ErrorClassificationUpdate request)
        => new() { Data = await service.Update(request) };
}
