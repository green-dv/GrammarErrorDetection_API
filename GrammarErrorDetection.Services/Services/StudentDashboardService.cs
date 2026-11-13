using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

/// <summary>
/// Dashboard del estudiante. El alcance es siempre el usuario autenticado: el controlador entrega el Id del token
/// y nunca uno recibido en la consulta.
/// </summary>
public sealed class StudentDashboardService(IStudentDashboardRepository repository) : IStudentDashboardService
{
    public Task<StudentDashboardDto> Summary(Guid userId, StudentDashboardQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return repository.Summary(userId, query.ClassId);
    }

    public Task<PagedList<FrequentErrorItem>> FrequentErrors(Guid userId, FrequentErrorQuery query)
        => repository.FrequentErrors(userId, query);

    public Task<StudentErrorsDashboardDto> Errors(Guid userId, StudentErrorsQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return repository.Errors(userId, query.ClassId);
    }

    public Task<PagedList<TextHistoryItem>> Texts(Guid userId, TextHistoryQuery query) => repository.Texts(userId, query);

    public Task<PagedList<ChallengeProgressItem>> Challenges(Guid userId, ChallengeProgressQuery query)
        => repository.Challenges(userId, query);

    public Task<PagedList<PointHistoryItem>> Points(Guid userId, PointHistoryQuery query) => repository.Points(userId, query);
}
