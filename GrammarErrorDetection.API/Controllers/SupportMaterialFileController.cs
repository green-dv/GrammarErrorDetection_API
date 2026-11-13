using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;
using GrammarErrorDetection.Models.Grammar.Validation;
using GrammarErrorDetection.Services.Configuration;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize(Roles = "Teacher,Coordinator")]
[Route("api/SupportMaterial/file")]
public sealed class SupportMaterialFileController(ISupportMaterialFileService files) : ControllerBase
{
    private const long RequestLimit = SupportMaterialFileOptions.MaxBytes + 64 * 1024;

    /// <summary>Sube un archivo y devuelve su enlace y tamaño (para luego crear o editar el material).</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimit)]
    public async Task<Wrapper<MaterialFileUploadResult>> Upload([FromForm] MaterialFileUpload request, CancellationToken cancellationToken)
    {
        await using var content = request.File.OpenReadStream();
        var data = await files.Store(content, request.File.FileName, request.File.Length, cancellationToken);
        Response.StatusCode = StatusCodes.Status201Created;
        return new() { Data = data };
    }

    /// <summary>
    /// Reemplaza el archivo de un material existente en un solo paso. Detecta el tipo por la extensión
    /// (debe existir en el catálogo de tipos de archivo) y actualiza enlace, tamaño y fecha de subida.
    /// Name es opcional; si se envía, también renombra el material.
    /// </summary>
    [HttpPut("~/api/SupportMaterial/{id:guid}/file")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimit)]
    public async Task<Wrapper<SupportMaterialEntity>> ReplaceFile([FromRoute, NotEmptyGuid] Guid id,
        [FromForm] MaterialFileReplace request, CancellationToken cancellationToken)
    {
        await using var content = request.File.OpenReadStream();
        var data = await files.ReplaceFile(id, content, request.File.FileName, request.File.Length, request.Name, cancellationToken);
        return new() { Data = data };
    }
}

public sealed class MaterialFileUpload
{
    [Required]
    public IFormFile File { get; set; } = null!;
}

public sealed class MaterialFileReplace
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [StringLength(255, MinimumLength = 1)]
    public string? Name { get; set; }
}
