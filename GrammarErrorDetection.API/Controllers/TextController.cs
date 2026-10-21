using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Texts;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class TextController(ITextService service) : ControllerBase
{
    [HttpGet]
    public async Task<Wrapper<List<TextListItemDto>>> List([FromQuery] PaginationDto<TextList> request)
    {
        var canViewAll = User.IsInRole("Teacher") || User.IsInRole("Coordinator");
        var data = await service.List(UserId(), canViewAll, request);
        return new Wrapper<List<TextListItemDto>> { Data = data, Count = data.Count };
    }

    [HttpPost("upload")]
    public async Task<Wrapper<TextListItemDto>> Upload([FromBody] TextUploadRequest request, CancellationToken cancellationToken)
    {
        var data = await service.Upload(UserId(), request, cancellationToken);
        Response.StatusCode = StatusCodes.Status201Created;
        return new Wrapper<TextListItemDto> { Data = data };
    }

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        && userId != Guid.Empty ? userId : throw new UnauthorizedAccessException();
}
