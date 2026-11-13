using GrammarErrorDetection.Models.Grammar.Dashboards;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IStaffDashboardService
{
    Task<TeacherDashboardDto> Teacher(Guid teacherId, TeacherDashboardQuery query);
    /// <summary>Un docente solo ve sus cursos; un coordinador puede filtrar por cualquier docente o ver todos.</summary>
    Task<PagedList<AtRiskStudent>> AtRisk(Guid currentUserId, bool isCoordinator, AtRiskQuery query);
    Task<CoordinatorDashboardDto> Coordinator(CoordinatorPeriodQuery query);
    Task<PagedList<ClassCoverageItem>> ClassCoverage(ClassCoverageQuery query);
    /// <summary>Solo coordinador: docentes ordenados por el promedio de nota final de sus estudiantes.</summary>
    Task<PagedList<TeacherRankingItem>> TeacherRanking(TeacherRankingQuery query);
}
