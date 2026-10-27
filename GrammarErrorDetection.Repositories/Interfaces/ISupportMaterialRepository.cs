using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface ISupportMaterialRepository
{
    Task<List<SupportMaterialListItemDto>> List(PaginationDto<SupportMaterialList> request);
    Task<SupportMaterialEntity> Insert(SupportMaterialInsert request);
    Task<SupportMaterialEntity> Update(SupportMaterialUpdate request);
}
