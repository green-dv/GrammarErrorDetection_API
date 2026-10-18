using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.VerificationCodes;

namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IVerificationCodeRepository
    {
        Task<List<VerificationCodeEntity>> List(PaginationDto<VerificationCodeList> request);
        Task<VerificationCodeEntity> Insert(VerificationCodeInsert request);
        Task<VerificationCodeEntity> Update(VerificationCodeUpdate request);
    }
}

