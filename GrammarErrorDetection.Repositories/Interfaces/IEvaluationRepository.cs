using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Evaluations;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IEvaluationRepository
{
    Task<List<EvaluationEntity>> List(PaginationDto<EvaluationList> request);
    Task<EvaluationEntity> Insert(EvaluationInsert request);
    Task<EvaluationEntity> Update(EvaluationUpdate request);
}
