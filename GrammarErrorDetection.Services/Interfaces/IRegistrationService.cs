using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Registrations;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface IRegistrationService
    {
        Task<List<RegistrationListItemDto>> List(PaginationDto<RegistrationSummaryList> request);
        async Task<PagedList<RegistrationListItemDto>> ListPage(PaginationDto<RegistrationSummaryList> request)
        {
            var items = await List(request);
            return new PagedList<RegistrationListItemDto>(items, items.Count);
        }
        Task<RegistrationEntity> Insert(RegistrationInsert request);
        Task<RegistrationEntity> Update(RegistrationUpdate request);
        Task<List<RegistrationListItemDto>> ListMine(Guid studentId, PaginationDto<RegistrationSummaryList> request);
        async Task<PagedList<RegistrationListItemDto>> ListMinePage(Guid studentId, PaginationDto<RegistrationSummaryList> request)
        {
            var items = await ListMine(studentId, request);
            return new PagedList<RegistrationListItemDto>(items, items.Count);
        }
        Task<List<StudentCourseHistoryDto>> ListStudentCourses(Guid studentId, StudentCourseHistoryQuery request);
        async Task<PagedList<StudentCourseHistoryDto>> ListStudentCoursesPage(Guid studentId, StudentCourseHistoryQuery request)
        {
            var items = await ListStudentCourses(studentId, request);
            return new PagedList<StudentCourseHistoryDto>(items, items.Count);
        }
        Task<List<AvailableRegistrationCourseDto>> ListAvailableCourses(Guid studentId, PaginationDto<AvailableRegistrationCourseList> request);
        Task<RegistrationEntity> Submit(Guid studentId, StudentRegistrationRequest request);
        Task<RegistrationEntity> Decide(Guid registrationId, RegistrationDecisionRequest request);
        Task<RegistrationGenerationResult> GenerateNextBimester(RegistrationGenerationRequest request);
    }
}

