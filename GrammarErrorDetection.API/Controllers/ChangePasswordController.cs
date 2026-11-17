using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Models.Grammar.Validation;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ChangePasswordController(IUserService service) : ControllerBase
{
    [HttpPut("{userId:guid}")]
    public async Task<Wrapper<bool>> Update([FromRoute, NotEmptyGuid] Guid userId,
        [FromBody] ChangePasswordRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var authenticatedId)
            || authenticatedId != userId)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return new Wrapper<bool>
            {
                Error = new APIError
                {
                    ErrorCode = StatusCodes.Status403Forbidden,
                    ErrorMessage = "Solo puedes cambiar tu propia contraseña."
                }
            };
        }

        await service.ChangePasswordAsync(userId, request);
        return new Wrapper<bool> { Data = true };
    }
}
