using GrammarErrorDetection.Models.Grammar.Dashboards;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IStudentDashboardRepository
{
    Task<StudentDashboardDto> Summary(Guid userId, Guid? classId);
    Task<PagedList<FrequentErrorItem>> FrequentErrors(Guid userId, FrequentErrorQuery query);
    Task<StudentErrorsDashboardDto> Errors(Guid userId, Guid? classId);
    Task<PagedList<TextHistoryItem>> Texts(Guid userId, TextHistoryQuery query);
    Task<PagedList<ChallengeProgressItem>> Challenges(Guid userId, ChallengeProgressQuery query);
    Task<PagedList<PointHistoryItem>> Points(Guid userId, PointHistoryQuery query);
}
