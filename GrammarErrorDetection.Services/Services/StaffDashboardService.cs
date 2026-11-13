using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

/// <summary>
/// Dashboards del docente y del coordinador. El alcance se decide aquí con el usuario del token:
/// un docente nunca puede consultar cursos de otro docente.
/// </summary>
public sealed class StaffDashboardService(IStaffDashboardRepository repository) : IStaffDashboardService
{
    public Task<TeacherDashboardDto> Teacher(Guid teacherId, TeacherDashboardQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return repository.Teacher(teacherId, query.ClassId);
    }

    public Task<PagedList<AtRiskStudent>> AtRisk(Guid currentUserId, bool isCoordinator, AtRiskQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (currentUserId == Guid.Empty) throw new ValidationException("El usuario actual no es válido.");
        // Docente: se ignora cualquier TeacherId recibido y se usa el suyo.
        var teacherId = isCoordinator ? query.TeacherId : currentUserId;
        return repository.AtRisk(teacherId, query);
    }

    public Task<CoordinatorDashboardDto> Coordinator(CoordinatorPeriodQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return repository.Coordinator(query.Year, query.Bimester);
    }

    public Task<PagedList<ClassCoverageItem>> ClassCoverage(ClassCoverageQuery query) => repository.ClassCoverage(query);

    public Task<PagedList<TeacherRankingItem>> TeacherRanking(TeacherRankingQuery query) => repository.TeacherRanking(query);
}
