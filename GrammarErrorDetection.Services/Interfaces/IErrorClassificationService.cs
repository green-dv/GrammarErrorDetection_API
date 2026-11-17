using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.Grammar.ErrorClassifications;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IErrorClassificationService
{
    Task<PagedList<ErrorClassificationEntity>> List(PaginationDto<ErrorClassificationList> request);
    Task<ErrorClassificationEntity> Get(Guid id);
    Task<ErrorClassificationEntity> Insert(ErrorClassificationInsert request);
    Task<ErrorClassificationEntity> Update(ErrorClassificationUpdate request);
}
