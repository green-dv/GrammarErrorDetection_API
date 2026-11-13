using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Services.Configuration;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize(Roles = "Coordinator")]
[Route("api/Achievement/icon")]
public sealed class AchievementIconController(IAchievementFileService files) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(AchievementFileOptions.MaxBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = AchievementFileOptions.MaxBytes + 64 * 1024)]
    public async Task<Wrapper<AchievementIconResult>> Upload([FromForm] AchievementIconUpload request, CancellationToken cancellationToken)
    {
        if (request.File.Length is 0 or > AchievementFileOptions.MaxBytes)
            throw new ValidationException("La imagen debe tener contenido y no superar 5 MiB.");
        await using var stream = request.File.OpenReadStream();
        var result = await files.Upload(stream, cancellationToken);
        Response.StatusCode = StatusCodes.Status201Created;
        return new() { Data = result };
    }
}

public sealed class AchievementIconUpload
{
    [Required]
    public IFormFile File { get; set; } = null!;
}
