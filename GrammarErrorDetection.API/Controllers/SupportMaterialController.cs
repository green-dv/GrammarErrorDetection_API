using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;
using GrammarErrorDetection.Models.Grammar.Validation;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class SupportMaterialController(ISupportMaterialService service) : ControllerBase
{
    [HttpGet]
    public async Task<Wrapper<List<SupportMaterialListItemDto>>> List([FromQuery] PaginationDto<SupportMaterialList> request)
    {
        var data = await service.List(request);
        return new Wrapper<List<SupportMaterialListItemDto>> { Data = data, Count = data.Count };
    }

    [HttpGet("{id:guid}")]
    public async Task<Wrapper<SupportMaterialListItemDto>> Get([FromRoute, NotEmptyGuid] Guid id)
    {
        var data = await service.Get(id);
        return new Wrapper<SupportMaterialListItemDto> { Data = data };
    }

    [HttpPost]
    [Authorize(Roles = "Teacher,Coordinator")]
    public async Task<Wrapper<SupportMaterialEntity>> Insert([FromBody] SupportMaterialInsert request)
    {
        var data = await service.Insert(request);
        Response.StatusCode = StatusCodes.Status201Created;
        return new Wrapper<SupportMaterialEntity> { Data = data };
    }

    [HttpPut]
    [Authorize(Roles = "Teacher,Coordinator")]
    public async Task<Wrapper<SupportMaterialEntity>> Update([FromBody] SupportMaterialUpdate request)
    {
        var data = await service.Update(request);
        return new Wrapper<SupportMaterialEntity> { Data = data };
    }
}
