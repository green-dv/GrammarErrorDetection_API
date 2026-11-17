using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.Grammar.ErrorClassifications;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IErrorClassificationRepository
{
    Task<PagedList<ErrorClassificationEntity>> List(PaginationDto<ErrorClassificationList> request);
    Task<List<ErrorClassificationEntity>> FindByName(string name);
    Task<ErrorClassificationEntity> Insert(ErrorClassificationInsert request);
    Task<ErrorClassificationEntity> Update(ErrorClassificationUpdate request);
}
