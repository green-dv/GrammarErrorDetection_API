using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Evaluations;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IEvaluationService
{
    Task<List<EvaluationEntity>> List(PaginationDto<EvaluationList> request);
    async Task<PagedList<EvaluationEntity>> ListPage(PaginationDto<EvaluationList> request)
    {
        var items = await List(request);
        return new PagedList<EvaluationEntity>(items, items.Count);
    }
    Task<EvaluationEntity> Insert(EvaluationInsert request);
    Task<EvaluationEntity> Update(EvaluationUpdate request);
}
