using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.ErrorClassifications;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IErrorClassificationService
{
    Task<List<ErrorClassificationEntity>> List(PaginationDto<ErrorClassificationList> request);
    Task<ErrorClassificationEntity> Get(Guid id);
}
