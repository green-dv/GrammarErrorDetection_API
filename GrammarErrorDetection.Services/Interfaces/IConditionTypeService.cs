using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IConditionTypeService
{
    Task<List<ConditionTypeEntity>> List(PaginationDto<ConditionTypeList> request);
    Task<ConditionTypeEntity> Insert(ConditionTypeInsert request);
    Task<ConditionTypeEntity> Update(ConditionTypeUpdate request);
}
