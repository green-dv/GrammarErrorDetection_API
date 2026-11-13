using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface ISupportMaterialRepository
{
    Task<List<SupportMaterialListItemDto>> List(PaginationDto<SupportMaterialList> request, Guid? viewerId = null);
    Task<SupportMaterialEntity> Insert(SupportMaterialInsert request);
    Task<SupportMaterialEntity> Update(SupportMaterialUpdate request);
    Task<List<SupportMaterialRecommendationRow>> ListRecommendations(Guid userId, Guid? classId,
        int categoryLimit, int materialsPerCategory);
    Task<SupportMaterialInteractionEntity> RegisterView(Guid userId, Guid supportMaterialId, Guid? registrationId, bool downloaded);
    Task<SupportMaterialInteractionEntity> SetFeedback(Guid userId, Guid supportMaterialId, bool helpful, string? comment);
}
