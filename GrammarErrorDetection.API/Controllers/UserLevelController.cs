using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.UserLevels;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class UserLevelController(IUserLevelService service) : ControllerBase
{
    [HttpGet]
    public async Task<Wrapper<List<UserLevelListItemDto>>> List([FromQuery] PaginationDto<UserLevelList> request)
    {
        var page = await service.List(request);
        return new() { Data = page.Items, Count = page.Items.Count, TotalCount = page.TotalCount };
    }

    // La lectura es de cualquier usuario autenticado (el perfil muestra los niveles); solo el coordinador los gestiona.
    // Al crear, editar o deshabilitar se reasigna el nivel de todos los usuarios.
    [HttpPost]
    [Authorize(Roles = "Coordinator")]
    public async Task<Wrapper<UserLevelListItemDto>> Insert([FromBody] UserLevelInsert request)
    {
        var data = await service.Insert(request);
        Response.StatusCode = StatusCodes.Status201Created;
        return new() { Data = data };
    }

    [HttpPut]
    [Authorize(Roles = "Coordinator")]
    public async Task<Wrapper<UserLevelListItemDto>> Update([FromBody] UserLevelUpdate request)
        => new() { Data = await service.Update(request) };
}
