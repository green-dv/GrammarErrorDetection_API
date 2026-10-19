using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Evaluations;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class EvaluationService : IEvaluationService
{
    private readonly IEvaluationRepository repository;

    public EvaluationService(IEvaluationRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        this.repository = repository;
    }

    public Task<List<EvaluationEntity>> List(PaginationDto<EvaluationList> request) => repository.List(request);
    public Task<EvaluationEntity> Insert(EvaluationInsert request) => repository.Insert(request);
    public Task<EvaluationEntity> Update(EvaluationUpdate request) => repository.Update(request);
}
