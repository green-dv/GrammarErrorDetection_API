using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Validation;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize(Roles = "Coordinator")]
[Route("api/[controller]")]
public sealed class RequirePasswordChangeController(IUserService service) : ControllerBase
{
    [HttpPut("{userId:guid}")]
    public async Task<Wrapper<bool>> Update([FromRoute, NotEmptyGuid] Guid userId)
    {
        await service.RequirePasswordChangeAsync(userId);
        return new Wrapper<bool> { Data = true };
    }
}
