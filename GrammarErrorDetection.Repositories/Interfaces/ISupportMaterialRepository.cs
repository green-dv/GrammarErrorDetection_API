using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface ISupportMaterialRepository
{
    Task<List<SupportMaterialListItemDto>> List(PaginationDto<SupportMaterialList> request, Guid? viewerId = null);
    /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina.</summary>
    async Task<PagedList<SupportMaterialListItemDto>> ListPage(PaginationDto<SupportMaterialList> request, Guid? viewerId = null)
    {
        var items = await List(request, viewerId);
        return new PagedList<SupportMaterialListItemDto>(items, items.Count);
    }
    Task<SupportMaterialEntity> Insert(SupportMaterialInsert request);
    Task<SupportMaterialEntity> Update(SupportMaterialUpdate request);
    Task<List<SupportMaterialRecommendationRow>> ListRecommendations(Guid userId, Guid? classId,
        int categoryLimit, int materialsPerCategory);
    Task<SupportMaterialInteractionEntity> RegisterView(Guid userId, Guid supportMaterialId, Guid? registrationId, bool downloaded);
    Task<SupportMaterialInteractionEntity> SetFeedback(Guid userId, Guid supportMaterialId, bool helpful, string? comment);
}
