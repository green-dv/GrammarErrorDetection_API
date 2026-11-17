using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.VerificationCodes;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface IVerificationCodeService
    {
        Task<List<VerificationCodeEntity>> List(PaginationDto<VerificationCodeList> request);
        async Task<PagedList<VerificationCodeEntity>> ListPage(PaginationDto<VerificationCodeList> request)
        {
            var items = await List(request);
            return new PagedList<VerificationCodeEntity>(items, items.Count);
        }
        Task<VerificationCodeEntity> Insert(VerificationCodeInsert request);
        Task<VerificationCodeEntity> Update(VerificationCodeUpdate request);
    }
}
