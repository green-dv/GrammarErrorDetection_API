using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;

namespace GrammarErrorDetection.Services.Interfaces;

public interface ISupportMaterialService
{
    // viewer: si es un estudiante, cada fila trae su interacción (visto / útil) y puede filtrar OnlyHelpful.
    Task<List<SupportMaterialListItemDto>> List(PaginationDto<SupportMaterialList> request, ClaimsPrincipal? viewer = null);
    Task<SupportMaterialListItemDto> Get(Guid id, ClaimsPrincipal? viewer = null);
    Task<SupportMaterialEntity> Insert(SupportMaterialInsert request);
    Task<SupportMaterialEntity> Update(SupportMaterialUpdate request);
}
