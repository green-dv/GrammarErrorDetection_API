using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Registrations;

namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IRegistrationRepository
    {
        Task<List<RegistrationListItemDto>> ListSummary(PaginationDto<RegistrationSummaryList> request);
        Task<List<RegistrationEntity>> List(PaginationDto<RegistrationList> request);
        Task<RegistrationEntity> Insert(RegistrationInsert request);
        Task<RegistrationEntity> Update(RegistrationUpdate request);
    }
}

