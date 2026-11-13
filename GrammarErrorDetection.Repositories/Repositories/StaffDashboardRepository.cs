using System.ComponentModel.DataAnnotations;
using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;
using static GrammarErrorDetection.Repositories.Utils.DataRowValue;

namespace GrammarErrorDetection.Repositories.Repositories;

/// <summary>Dashboards del docente y del coordinador: resúmenes con varios cursores y listados de dos DataTables.</summary>
public sealed class StaffDashboardRepository(IStoredProcedureMultiResultExecutor multi, IStoredProcedurePageExecutor pages)
    : IStaffDashboardRepository
{
    private static readonly string[] TeacherCursors = ["Kpis", "Courses", "TopCategories", "MaterialUsage", "WeeklyActivity", "ModulePerformance"];
    private static readonly string[] CoordinatorCursors = ["Kpis", "Gamification", "ErrorsByModule", "TopCategories", "ModulePerformance"];

    public async Task<TeacherDashboardDto> Teacher(Guid teacherId, Guid? classId)
    {
        if (teacherId == System.Guid.Empty) throw new ValidationException("TeacherId debe identificar al docente actual.");
        if (classId == System.Guid.Empty) throw new ValidationException("ClassId no puede ser vacío.");
        var response = await multi.ExecuteMultiResultStoredProcedureAsync("grammar", "Dashboard_Teacher",
            [new("TeacherId", NpgsqlDbType.Uuid, teacherId), new("ClassId", NpgsqlDbType.Uuid, classId),
             .. Cursors(TeacherCursors, "dashboard_teacher_")], TeacherCursors);
        using var tables = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió resultados.");
        return new TeacherDashboardDto
        {
            Kpis = tables["Kpis"].AsEnumerable().Select(row => new TeacherDashboardKpis
            {
                FirstName = String(row, "FirstName") ?? string.Empty,
                LastName = String(row, "LastName") ?? string.Empty,
                AtRiskNoTexts = Long(row, "AtRiskNoTexts"),
                AtRiskLowScore = Long(row, "AtRiskLowScore"),
                TextsLast30 = Long(row, "TextsLast30"),
                TextsPrevious30 = Long(row, "TextsPrevious30"),
                AccuracyLast30 = Decimal(row, "AccuracyLast30"),
                AccuracyPrevious30 = Decimal(row, "AccuracyPrevious30"),
                CourseCount = Int(row, "CourseCount"),
                EnrolledStudents = Long(row, "EnrolledStudents"),
                StudentsWithTexts = Long(row, "StudentsWithTexts"),
                CoveragePercentage = Decimal(row, "CoveragePercentage"),
                AnalyzedTexts = Long(row, "AnalyzedTexts"),
                AccuracyPercentage = Decimal(row, "AccuracyPercentage"),
                ErrorsPerText = Decimal(row, "ErrorsPerText"),
                AverageFinalScore = Decimal(row, "AverageFinalScore"),
                PassRatePercentage = Decimal(row, "PassRatePercentage"),
                GradedStudents = Long(row, "GradedStudents"),
                AtRiskStudents = Long(row, "AtRiskStudents")
            }).FirstOrDefault(),
            Courses = tables["Courses"].AsEnumerable().Select(row => new TeacherCourseSummary
            {
                ClassId = Guid(row, "ClassId") ?? throw new DataException("Curso sin Id."),
                ModuleName = String(row, "ModuleName") ?? string.Empty,
                ModuleLevel = Int(row, "ModuleLevel"),
                Year = Int(row, "Year"),
                Bimester = Int(row, "Bimester"),
                Parallel = String(row, "Parallel"),
                StartTime = Time(row, "StartTime"),
                EndTime = Time(row, "EndTime"),
                EnrolledStudents = Long(row, "EnrolledStudents"),
                StudentsWithTexts = Long(row, "StudentsWithTexts"),
                CoveragePercentage = Decimal(row, "CoveragePercentage"),
                AnalyzedTexts = Long(row, "AnalyzedTexts"),
                AccuracyPercentage = Decimal(row, "AccuracyPercentage"),
                ErrorsPerText = Decimal(row, "ErrorsPerText"),
                Exam1Average = Decimal(row, "Exam1Average"),
                Exam2Average = Decimal(row, "Exam2Average"),
                FinalAverage = Decimal(row, "FinalAverage"),
                PassRatePercentage = Decimal(row, "PassRatePercentage"),
                AtRiskStudents = Long(row, "AtRiskStudents")
            }).ToList(),
            TopCategories = tables["TopCategories"].AsEnumerable().Select(row => new StaffErrorCategory
            {
                ErrorClassificationId = Guid(row, "ErrorClassificationId") ?? throw new DataException("Categoría sin Id."),
                ErrorClassificationName = String(row, "ErrorClassificationName") ?? string.Empty,
                ErrorCount = Long(row, "ErrorCount"),
                ErrorPercentage = Decimal(row, "ErrorPercentage"),
                AffectedStudents = Long(row, "AffectedStudents"),
                AffectedStudentPercentage = Decimal(row, "AffectedStudentPercentage")
            }).ToList(),
            MaterialUsage = tables["MaterialUsage"].AsEnumerable().Select(row => new MaterialUsageItem
            {
                SupportMaterialId = Guid(row, "SupportMaterialId") ?? throw new DataException("Material sin Id."),
                Name = String(row, "Name") ?? string.Empty,
                ErrorClassificationName = String(row, "ErrorClassificationName") ?? string.Empty,
                Viewers = Long(row, "Viewers"),
                Views = Long(row, "Views"),
                HelpfulVotes = Long(row, "HelpfulVotes"),
                NotHelpfulVotes = Long(row, "NotHelpfulVotes")
            }).ToList(),
            WeeklyActivity = tables["WeeklyActivity"].AsEnumerable().Select(row => new TeacherWeeklyActivity
            {
                WeekStart = Date(row, "WeekStart") ?? throw new DataException("Semana sin fecha."),
                AnalyzedTexts = Long(row, "AnalyzedTexts"),
                ActiveStudents = Long(row, "ActiveStudents"),
                AccuracyPercentage = Decimal(row, "AccuracyPercentage"),
                ErrorsPerText = Decimal(row, "ErrorsPerText")
            }).ToList(),
            ModulePerformance = tables["ModulePerformance"].AsEnumerable().Select(ReadModule).ToList()
        };
    }

    public async Task<PagedList<AtRiskStudent>> AtRisk(Guid? teacherId, AtRiskQuery query)
    {
        RepositoryValidation.ValidateInput(query);
        if (teacherId == System.Guid.Empty || query.ClassId == System.Guid.Empty)
            throw new ValidationException("TeacherId y ClassId no pueden ser vacíos.");
        if (query.Reason is not null && !AtRiskQuery.Reasons.Contains(query.Reason, StringComparer.Ordinal))
            throw new ValidationException("Reason debe ser NoTexts o LowScore.");
        RequirePeriod(query.Year, query.Bimester);
        return await Page("Dashboard_AtRisk_List", "dashboard_atrisk_",
            [new("TeacherId", NpgsqlDbType.Uuid, teacherId), new("ClassId", NpgsqlDbType.Uuid, query.ClassId),
             new("Reason", NpgsqlDbType.Text, query.Reason), new("Year", NpgsqlDbType.Integer, query.Year),
             new("Bimester", NpgsqlDbType.Integer, query.Bimester)], query, row => new AtRiskStudent
            {
                UserId = Guid(row, "UserId") ?? throw new DataException("Estudiante sin Id."),
                StudentName = String(row, "StudentName") ?? string.Empty,
                Email = String(row, "Email"),
                ClassId = Guid(row, "ClassId") ?? throw new DataException("Curso sin Id."),
                ModuleName = String(row, "ModuleName") ?? string.Empty,
                Year = Int(row, "Year"),
                Bimester = Int(row, "Bimester"),
                Parallel = String(row, "Parallel"),
                Reason = String(row, "Reason") ?? string.Empty,
                Exam1Score = Decimal(row, "Exam1Score"),
                Exam2Score = Decimal(row, "Exam2Score"),
                FinalScore = Decimal(row, "FinalScore"),
                AnalyzedTexts = Long(row, "AnalyzedTexts"),
                LastTextAt = Date(row, "LastTextAt"),
                AccuracyPercentage = Decimal(row, "AccuracyPercentage")
            });
    }

    public async Task<CoordinatorDashboardDto> Coordinator(int? year, int? bimester)
    {
        RequirePeriod(year, bimester);
        var response = await multi.ExecuteMultiResultStoredProcedureAsync("grammar", "Dashboard_Coordinator",
            [new("Year", NpgsqlDbType.Integer, year), new("Bimester", NpgsqlDbType.Integer, bimester),
             .. Cursors(CoordinatorCursors, "dashboard_coordinator_")], CoordinatorCursors);
        using var tables = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió resultados.");
        return new CoordinatorDashboardDto
        {
            Kpis = tables["Kpis"].AsEnumerable().Select(row => new CoordinatorDashboardKpis
            {
                ActiveStudents = Long(row, "ActiveStudents"),
                ActiveTeachers = Long(row, "ActiveTeachers"),
                ActiveCoordinators = Long(row, "ActiveCoordinators"),
                PendingRegistrations = Long(row, "PendingRegistrations"),
                CourseCount = Int(row, "CourseCount"),
                EnrolledStudents = Long(row, "EnrolledStudents"),
                StudentsWithTexts = Long(row, "StudentsWithTexts"),
                CoveragePercentage = Decimal(row, "CoveragePercentage"),
                AnalyzedTexts = Long(row, "AnalyzedTexts"),
                AccuracyPercentage = Decimal(row, "AccuracyPercentage"),
                ErrorsPerText = Decimal(row, "ErrorsPerText"),
                AverageFinalScore = Decimal(row, "AverageFinalScore"),
                PassRatePercentage = Decimal(row, "PassRatePercentage"),
                AtRiskStudents = Long(row, "AtRiskStudents"),
                ClassesWithActivity = Long(row, "ClassesWithActivity"),
                AtRiskNoTexts = Long(row, "AtRiskNoTexts"),
                AtRiskLowScore = Long(row, "AtRiskLowScore"),
                AtRiskPercentage = Decimal(row, "AtRiskPercentage"),
                PreviousCourseCount = IntOrNull(row, "PreviousCourseCount"),
                PreviousEnrolledStudents = LongOrNull(row, "PreviousEnrolledStudents"),
                PreviousAnalyzedTexts = LongOrNull(row, "PreviousAnalyzedTexts"),
                PreviousAccuracyPercentage = Decimal(row, "PreviousAccuracyPercentage"),
                PreviousAtRiskPercentage = Decimal(row, "PreviousAtRiskPercentage"),
                PreviousAverageFinalScore = Decimal(row, "PreviousAverageFinalScore"),
                PreviousPassRatePercentage = Decimal(row, "PreviousPassRatePercentage"),
                PreviousErrorsPerText = Decimal(row, "PreviousErrorsPerText"),
                GradedStudents = Long(row, "GradedStudents")
            }).FirstOrDefault(),
            Gamification = tables["Gamification"].AsEnumerable().Select(row => new CoordinatorGamificationSummary
            {
                AchievementsAwarded = Long(row, "AchievementsAwarded"),
                PointsAwarded = Long(row, "PointsAwarded"),
                StudentsWithPoints = Long(row, "StudentsWithPoints"),
                ChallengesCompleted = Long(row, "ChallengesCompleted"),
                ActiveChallenges = Long(row, "ActiveChallenges"),
                ActiveAchievements = Long(row, "ActiveAchievements"),
                PreviousAchievementsAwarded = LongOrNull(row, "PreviousAchievementsAwarded"),
                PreviousPointsAwarded = LongOrNull(row, "PreviousPointsAwarded"),
                PreviousChallengesCompleted = LongOrNull(row, "PreviousChallengesCompleted")
            }).FirstOrDefault(),
            ErrorsByModule = tables["ErrorsByModule"].AsEnumerable().Select(row => new ModuleErrorCategory
            {
                ModuleNameId = Guid(row, "ModuleNameId") ?? throw new DataException("Módulo sin Id."),
                ModuleName = String(row, "ModuleName") ?? string.Empty,
                ModuleLevel = Int(row, "ModuleLevel"),
                ErrorClassificationId = Guid(row, "ErrorClassificationId") ?? throw new DataException("Categoría sin Id."),
                ErrorClassificationName = String(row, "ErrorClassificationName") ?? string.Empty,
                Rank = Int(row, "Rank"),
                ErrorCount = Long(row, "ErrorCount"),
                ModuleErrorCount = Long(row, "ModuleErrorCount"),
                ErrorPercentage = Decimal(row, "ErrorPercentage")
            }).ToList(),
            TopCategories = tables["TopCategories"].AsEnumerable().Select(row => new InstitutionErrorCategory
            {
                ErrorClassificationId = Guid(row, "ErrorClassificationId") ?? throw new DataException("Categoría sin Id."),
                ErrorClassificationName = String(row, "ErrorClassificationName") ?? string.Empty,
                ErrorCount = Long(row, "ErrorCount"),
                ErrorPercentage = Decimal(row, "ErrorPercentage"),
                ModuleCount = Long(row, "ModuleCount")
            }).ToList(),
            ModulePerformance = tables["ModulePerformance"].AsEnumerable().Select(ReadModule).ToList()
        };
    }

    public async Task<PagedList<TeacherRankingItem>> TeacherRanking(TeacherRankingQuery query)
    {
        RepositoryValidation.ValidateInput(query);
        RequirePeriod(query.Year, query.Bimester);
        if (!TeacherRankingQuery.Orders.Contains(query.Order, StringComparer.Ordinal))
            throw new ValidationException("Order debe ser Best o Worst.");
        return await Page("Dashboard_TeacherRanking_List", "dashboard_teacherranking_",
            [new("Year", NpgsqlDbType.Integer, query.Year), new("Bimester", NpgsqlDbType.Integer, query.Bimester),
             new("Order", NpgsqlDbType.Text, query.Order)], query, row => new TeacherRankingItem
            {
                TeacherId = Guid(row, "TeacherId") ?? throw new DataException("Docente sin Id."),
                TeacherName = String(row, "TeacherName") ?? string.Empty,
                CourseCount = Long(row, "CourseCount"),
                EnrolledStudents = Long(row, "EnrolledStudents"),
                GradedStudents = Long(row, "GradedStudents"),
                FinalAverage = Decimal(row, "FinalAverage"),
                PassRatePercentage = Decimal(row, "PassRatePercentage"),
                AnalyzedTexts = Long(row, "AnalyzedTexts"),
                TotalErrors = Long(row, "TotalErrors"),
                ErrorsPerText = Decimal(row, "ErrorsPerText"),
                AccuracyPercentage = Decimal(row, "AccuracyPercentage"),
                AtRiskStudents = Long(row, "AtRiskStudents"),
                Rank = Long(row, "Rank")
            });
    }

    private static ModulePerformanceItem ReadModule(DataRow row) => new()
    {
        ModuleNameId = Guid(row, "ModuleNameId") ?? throw new DataException("Módulo sin Id."),
        ModuleName = String(row, "ModuleName") ?? string.Empty,
        ModuleLevel = Int(row, "ModuleLevel"),
        CourseCount = Long(row, "CourseCount"),
        EnrolledStudents = Long(row, "EnrolledStudents"),
        GradedStudents = Long(row, "GradedStudents"),
        Exam1Average = Decimal(row, "Exam1Average"),
        Exam2Average = Decimal(row, "Exam2Average"),
        FinalAverage = Decimal(row, "FinalAverage"),
        PassRatePercentage = Decimal(row, "PassRatePercentage"),
        AnalyzedTexts = Long(row, "AnalyzedTexts"),
        TotalErrors = Long(row, "TotalErrors"),
        ErrorsPerText = Decimal(row, "ErrorsPerText"),
        AccuracyPercentage = Decimal(row, "AccuracyPercentage"),
        TopErrorClassificationName = String(row, "TopErrorClassificationName")
    };

    public async Task<PagedList<ClassCoverageItem>> ClassCoverage(ClassCoverageQuery query)
    {
        RepositoryValidation.ValidateInput(query);
        RequirePeriod(query.Year, query.Bimester);
        if (query.TeacherId == System.Guid.Empty) throw new ValidationException("TeacherId no puede ser vacío.");
        return await Page("Dashboard_ClassCoverage_List", "dashboard_classcoverage_",
            [new("Year", NpgsqlDbType.Integer, query.Year), new("Bimester", NpgsqlDbType.Integer, query.Bimester),
             new("TeacherId", NpgsqlDbType.Uuid, query.TeacherId)], query, row => new ClassCoverageItem
            {
                ClassId = Guid(row, "ClassId") ?? throw new DataException("Curso sin Id."),
                ModuleName = String(row, "ModuleName") ?? string.Empty,
                ModuleLevel = Int(row, "ModuleLevel"),
                Year = Int(row, "Year"),
                Bimester = Int(row, "Bimester"),
                Parallel = String(row, "Parallel"),
                TeacherId = Guid(row, "TeacherId") ?? throw new DataException("Curso sin docente."),
                TeacherName = String(row, "TeacherName") ?? string.Empty,
                EnrolledStudents = Long(row, "EnrolledStudents"),
                StudentsWithTexts = Long(row, "StudentsWithTexts"),
                CoveragePercentage = Decimal(row, "CoveragePercentage"),
                AnalyzedTexts = Long(row, "AnalyzedTexts"),
                AccuracyPercentage = Decimal(row, "AccuracyPercentage"),
                ErrorsPerText = Decimal(row, "ErrorsPerText"),
                AverageFinalScore = Decimal(row, "AverageFinalScore"),
                AtRiskStudents = Long(row, "AtRiskStudents")
            });
    }

    private static void RequirePeriod(int? year, int? bimester)
    {
        if (year is < 1) throw new ValidationException("Year debe ser mayor que cero.");
        if (bimester is < 1 or > 6) throw new ValidationException("Bimester debe estar entre 1 y 6.");
        if (bimester is not null && year is null) throw new ValidationException("Bimester requiere Year.");
    }

    private async Task<PagedList<T>> Page<T>(string procedure, string cursorPrefix, List<StoredProcedureParameter> filters,
        DashboardPageQuery query, Func<DataRow, T> map)
    {
        filters.Add(new("PageNumber", NpgsqlDbType.Integer, query.PageNumber));
        filters.Add(new("PageSize", NpgsqlDbType.Integer, query.PageSize));
        filters.Add(new("Results", NpgsqlDbType.Refcursor, cursorPrefix + "list", ParameterDirection.InputOutput));
        filters.Add(new("Total", NpgsqlDbType.Refcursor, cursorPrefix + "total", ParameterDirection.InputOutput));
        var response = await pages.ExecutePagedStoredProcedureAsync("grammar", procedure, filters);
        using var page = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió la página.");
        return new PagedList<T>(page.Rows.AsEnumerable().Select(map).ToList(), page.TotalCount);
    }

    private static IEnumerable<StoredProcedureParameter> Cursors(IEnumerable<string> names, string prefix)
        => names.Select(name => new StoredProcedureParameter(name, NpgsqlDbType.Refcursor,
            prefix + name.ToLowerInvariant(), ParameterDirection.InputOutput));
}
