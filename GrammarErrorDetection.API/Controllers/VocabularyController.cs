using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Vocabulary;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class VocabularyController(IVocabularyService service) : ControllerBase
{
    /// <summary>
    /// Palabras con error de vocabulario de cada oración de un texto, con alternativas y definiciones.
    /// Un estudiante solo consulta sus textos; docentes y coordinadores, los de cualquier usuario.
    /// </summary>
    [HttpGet("text/{textId:guid}")]
    [EnableRateLimiting("vocabulary")]
    public async Task<Wrapper<VocabularyAnalysisDto>> Analyze(Guid textId, CancellationToken cancellationToken)
    {
        var canViewAll = User.IsInRole("Teacher") || User.IsInRole("Coordinator");
        var data = await service.Analyze(UserId(), canViewAll, textId, cancellationToken);
        return new Wrapper<VocabularyAnalysisDto> { Data = data };
    }

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        && userId != Guid.Empty ? userId : throw new UnauthorizedAccessException();
}
