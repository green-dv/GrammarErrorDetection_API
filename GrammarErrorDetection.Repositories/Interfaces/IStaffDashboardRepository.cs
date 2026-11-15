using GrammarErrorDetection.Models.Grammar.Dashboards;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IStaffDashboardRepository
{
    Task<TeacherDashboardDto> Teacher(Guid teacherId, Guid? classId);
    /// <summary>teacherId null consulta todos los cursos (solo coordinadores).</summary>
    Task<PagedList<AtRiskStudent>> AtRisk(Guid? teacherId, AtRiskQuery query);
    Task<CoordinatorDashboardDto> Coordinator(int? year, int? bimester);
    Task<PagedList<ClassCoverageItem>> ClassCoverage(ClassCoverageQuery query);
    Task<PagedList<TeacherRankingItem>> TeacherRanking(TeacherRankingQuery query);
}
