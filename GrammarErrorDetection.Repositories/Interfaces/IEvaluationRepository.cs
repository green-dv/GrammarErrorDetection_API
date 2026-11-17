using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Evaluations;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IEvaluationRepository
{
    Task<List<EvaluationEntity>> List(PaginationDto<EvaluationList> request);
    /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina (los repositorios reales lo sobrescriben).</summary>
    async Task<PagedList<EvaluationEntity>> ListPage(PaginationDto<EvaluationList> request)
    {
        var items = await List(request);
        return new PagedList<EvaluationEntity>(items, items.Count);
    }
    Task<EvaluationEntity> Insert(EvaluationInsert request);
    Task<EvaluationEntity> Update(EvaluationUpdate request);
}
