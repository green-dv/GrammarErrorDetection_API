using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.VerificationCodes;

namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IVerificationCodeRepository
    {
        Task<List<VerificationCodeEntity>> List(PaginationDto<VerificationCodeList> request);
        /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina (los repositorios reales lo sobrescriben).</summary>
        async Task<PagedList<VerificationCodeEntity>> ListPage(PaginationDto<VerificationCodeList> request)
        {
            var items = await List(request);
            return new PagedList<VerificationCodeEntity>(items, items.Count);
        }
        Task<VerificationCodeEntity> Insert(VerificationCodeInsert request);
        Task<VerificationCodeEntity> Update(VerificationCodeUpdate request);
    }
}

