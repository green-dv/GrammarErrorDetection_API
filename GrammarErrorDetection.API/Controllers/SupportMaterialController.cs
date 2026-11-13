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
public sealed class SupportMaterialController(ISupportMaterialService service,
    ISupportMaterialRecommendationService recommendations, ISupportMaterialInteractionService interactions) : ControllerBase
{
    /// <summary>Material de apoyo recomendado según los errores gramaticales más frecuentes del estudiante.</summary>
    [HttpGet("recommendations")]
    [Authorize(Roles = "Student,Teacher,Coordinator")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<Wrapper<List<SupportMaterialRecommendationDto>>> Recommendations(
        [FromQuery] SupportMaterialRecommendationQuery request)
    {
        var data = await recommendations.Recommend(User, request);
        return new Wrapper<List<SupportMaterialRecommendationDto>> { Data = data, Count = data.Count };
    }

    /// <summary>Registra que el estudiante abrió (o descargó) un material de apoyo.</summary>
    [HttpPost("{id:guid}/view")]
    [Authorize(Roles = "Student")]
    public async Task<Wrapper<SupportMaterialInteractionEntity>> RegisterView([FromRoute, NotEmptyGuid] Guid id,
        [FromBody] SupportMaterialViewRequest request)
    {
        var data = await interactions.RegisterView(User, id, request);
        return new Wrapper<SupportMaterialInteractionEntity> { Data = data };
    }

    /// <summary>Guarda si el material le sirvió al estudiante. Requiere haberlo abierto antes.</summary>
    [HttpPut("{id:guid}/feedback")]
    [Authorize(Roles = "Student")]
    public async Task<Wrapper<SupportMaterialInteractionEntity>> SetFeedback([FromRoute, NotEmptyGuid] Guid id,
        [FromBody] SupportMaterialFeedbackRequest request)
    {
        var data = await interactions.SetFeedback(User, id, request);
        return new Wrapper<SupportMaterialInteractionEntity> { Data = data };
    }

    [HttpGet]
    public async Task<Wrapper<List<SupportMaterialListItemDto>>> List([FromQuery] PaginationDto<SupportMaterialList> request)
    {
        var data = await service.List(request, User);
        return new Wrapper<List<SupportMaterialListItemDto>> { Data = data, Count = data.Count };
    }

    [HttpGet("{id:guid}")]
    public async Task<Wrapper<SupportMaterialListItemDto>> Get([FromRoute, NotEmptyGuid] Guid id)
    {
        var data = await service.Get(id, User);
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
