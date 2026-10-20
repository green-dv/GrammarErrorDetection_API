using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Registrations;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface IRegistrationService
    {
        Task<List<RegistrationListItemDto>> List(PaginationDto<RegistrationSummaryList> request);
        Task<RegistrationEntity> Insert(RegistrationInsert request);
        Task<RegistrationEntity> Update(RegistrationUpdate request);
        Task<List<RegistrationListItemDto>> ListMine(Guid studentId, PaginationDto<RegistrationSummaryList> request);
        Task<List<StudentCourseHistoryDto>> ListStudentCourses(Guid studentId, StudentCourseHistoryQuery request);
        Task<List<AvailableRegistrationCourseDto>> ListAvailableCourses(Guid studentId, PaginationDto<AvailableRegistrationCourseList> request);
        Task<RegistrationEntity> Submit(Guid studentId, StudentRegistrationRequest request);
        Task<RegistrationEntity> Decide(Guid registrationId, RegistrationDecisionRequest request);
        Task<RegistrationGenerationResult> GenerateNextBimester(RegistrationGenerationRequest request);
    }
}

