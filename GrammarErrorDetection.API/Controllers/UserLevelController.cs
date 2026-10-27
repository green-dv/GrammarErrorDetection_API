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
        var data = await service.List(request);
        return new() { Data = data, Count = data.Count };
    }
}
