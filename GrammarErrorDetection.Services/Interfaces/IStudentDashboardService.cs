using GrammarErrorDetection.Models.Grammar.Dashboards;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IStudentDashboardService
{
    Task<StudentDashboardDto> Summary(Guid userId, StudentDashboardQuery query);
    Task<PagedList<FrequentErrorItem>> FrequentErrors(Guid userId, FrequentErrorQuery query);
    Task<StudentErrorsDashboardDto> Errors(Guid userId, StudentErrorsQuery query);
    Task<PagedList<TextHistoryItem>> Texts(Guid userId, TextHistoryQuery query);
    Task<PagedList<ChallengeProgressItem>> Challenges(Guid userId, ChallengeProgressQuery query);
    Task<PagedList<PointHistoryItem>> Points(Guid userId, PointHistoryQuery query);
}
