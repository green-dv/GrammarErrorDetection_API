using GrammarErrorDetection.Models.Grammar.ErrorClassifications;
using GrammarErrorDetection.Models.APIRequest;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IErrorClassificationRepository
{
    Task<List<ErrorClassificationEntity>> List(PaginationDto<ErrorClassificationList> request);
    Task<List<ErrorClassificationEntity>> FindByName(string name);
}
