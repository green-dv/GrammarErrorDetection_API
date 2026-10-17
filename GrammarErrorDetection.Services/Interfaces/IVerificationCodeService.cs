using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.VerificationCodes;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface IVerificationCodeService
    {
        Task<List<VerificationCodeEntity>> List(PaginationDto<VerificationCodeList> request);
        Task<VerificationCodeEntity> Insert(VerificationCodeInsert request);
        Task<VerificationCodeEntity> Update(VerificationCodeUpdate request);
    }
}
