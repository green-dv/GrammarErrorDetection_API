using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;

namespace GrammarErrorDetection.Services.Interfaces;

public interface ISupportMaterialService
{
    Task<List<SupportMaterialListItemDto>> List(PaginationDto<SupportMaterialList> request);
    Task<SupportMaterialListItemDto> Get(Guid id);
    Task<SupportMaterialEntity> Insert(SupportMaterialInsert request);
    Task<SupportMaterialEntity> Update(SupportMaterialUpdate request);
}
