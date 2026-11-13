using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class ConditionTypeService(IConditionTypeRepository repository) : IConditionTypeService
{
    public Task<List<ConditionTypeEntity>> List(PaginationDto<ConditionTypeList> request) => repository.List(request);
    public Task<ConditionTypeEntity> Insert(ConditionTypeInsert request) => repository.Insert(request);
    public Task<ConditionTypeEntity> Update(ConditionTypeUpdate request) => repository.Update(request);
}
