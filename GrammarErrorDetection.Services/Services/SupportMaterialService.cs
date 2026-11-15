using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class SupportMaterialService : ISupportMaterialService
{
    private readonly ISupportMaterialRepository repository;

    public SupportMaterialService(ISupportMaterialRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        this.repository = repository;
    }

    public Task<List<SupportMaterialListItemDto>> List(PaginationDto<SupportMaterialList> request, ClaimsPrincipal? viewer = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var viewerId = StudentId(viewer);
        if (request.Filter?.OnlyHelpful == true && viewerId is null)
            throw new ValidationException("Solo los estudiantes pueden filtrar los materiales que marcaron como útiles.");
        return repository.List(request, viewerId);
    }

    // Solo el estudiante tiene interacciones propias (vistas y "útil"); para otros roles no se consulta.
    private static Guid? StudentId(ClaimsPrincipal? viewer) =>
        viewer?.Identity?.IsAuthenticated == true && viewer.IsInRole("Student")
        && Guid.TryParse(viewer.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) && id != Guid.Empty
            ? id : null;

    public async Task<SupportMaterialListItemDto> Get(Guid id, ClaimsPrincipal? viewer = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("El identificador de material no puede estar vacío.", nameof(id));
        var materials = await repository.List(new PaginationDto<SupportMaterialList>
        {
            PageNumber = 1, PageSize = 1, Filter = new SupportMaterialList { Id = id }
        }, StudentId(viewer));
        return materials.SingleOrDefault(item => item.Id == id)
            ?? throw new KeyNotFoundException("No se encontró el material de apoyo.");
    }

    public Task<SupportMaterialEntity> Insert(SupportMaterialInsert request) => repository.Insert(request);
    public Task<SupportMaterialEntity> Update(SupportMaterialUpdate request) => repository.Update(request);
}
