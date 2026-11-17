using GrammarErrorDetection.Models.Grammar.Dashboards;
using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;

namespace GrammarErrorDetection.Services.Interfaces;

public interface ISupportMaterialService
{
    // viewer: si es un estudiante, cada fila trae su interacción (visto / útil) y puede filtrar OnlyHelpful.
    Task<List<SupportMaterialListItemDto>> List(PaginationDto<SupportMaterialList> request, ClaimsPrincipal? viewer = null);
    async Task<PagedList<SupportMaterialListItemDto>> ListPage(PaginationDto<SupportMaterialList> request, ClaimsPrincipal? viewer = null)
    {
        var items = await List(request, viewer);
        return new PagedList<SupportMaterialListItemDto>(items, items.Count);
    }
    Task<SupportMaterialListItemDto> Get(Guid id, ClaimsPrincipal? viewer = null);
    Task<SupportMaterialEntity> Insert(SupportMaterialInsert request);
    Task<SupportMaterialEntity> Update(SupportMaterialUpdate request);
}
