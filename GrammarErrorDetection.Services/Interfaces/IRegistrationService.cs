using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Registrations;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface IRegistrationService
    {
        Task<List<RegistrationEntity>> List(PaginationDto<RegistrationList> request);
        Task<RegistrationEntity> Insert(RegistrationInsert request);
        Task<RegistrationEntity> Update(RegistrationUpdate request);
        Task<RegistrationGenerationResult> GenerateNextBimester(RegistrationGenerationRequest request);
    }
}

