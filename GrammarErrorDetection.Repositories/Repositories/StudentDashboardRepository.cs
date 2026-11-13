using System.ComponentModel.DataAnnotations;
using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;
using static GrammarErrorDetection.Repositories.Utils.DataRowValue;

namespace GrammarErrorDetection.Repositories.Repositories;

/// <summary>
/// Dashboard del estudiante: los resúmenes usan el ejecutor de varios cursores y los listados el de dos DataTables.
/// </summary>
public sealed class StudentDashboardRepository(IStoredProcedureMultiResultExecutor multi, IStoredProcedurePageExecutor pages)
    : IStudentDashboardRepository
{
    private static readonly string[] SummaryCursors = ["Kpis", "ErrorsByCourse", "TopCategories", "NextAchievements", "DailyActivity"];
    private static readonly string[] ErrorCursors = ["Kpis", "TopCategories"];

    public async Task<StudentDashboardDto> Summary(Guid userId, Guid? classId)
    {
        RequireUser(userId);
        if (classId == System.Guid.Empty) throw new ValidationException("ClassId no puede ser vacío.");
        var response = await multi.ExecuteMultiResultStoredProcedureAsync("grammar", "Dashboard_Student",
            [new("UserId", NpgsqlDbType.Uuid, userId), new("ClassId", NpgsqlDbType.Uuid, classId),
             .. Cursors(SummaryCursors, "dashboard_student_")], SummaryCursors);
        using var tables = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió resultados.");
        return new StudentDashboardDto
        {
            Kpis = tables["Kpis"].AsEnumerable().Select(ReadKpis).FirstOrDefault(),
            ErrorsByCourse = tables["ErrorsByCourse"].AsEnumerable().Select(ReadCourse).ToList(),
            TopCategories = tables["TopCategories"].AsEnumerable().Select(row =>
            {
                var category = ReadCategory(row);
                category.CourseAverage = Decimal(row, "CourseAverage");
                return category;
            }).ToList(),
            NextAchievements = tables["NextAchievements"].AsEnumerable().Select(ReadAchievement).ToList(),
            DailyActivity = tables["DailyActivity"].AsEnumerable().Select(row => new DashboardDailyActivity
            {
                Day = Date(row, "Day") ?? throw new DataException("Día sin fecha."),
                AnalyzedTexts = Long(row, "AnalyzedTexts"),
                TotalErrors = Long(row, "TotalErrors")
            }).ToList()
        };
    }

    public async Task<StudentErrorsDashboardDto> Errors(Guid userId, Guid? classId)
    {
        RequireUser(userId);
        if (classId == System.Guid.Empty) throw new ValidationException("ClassId no puede ser vacío.");
        var response = await multi.ExecuteMultiResultStoredProcedureAsync("grammar", "Dashboard_StudentErrors",
            [new("UserId", NpgsqlDbType.Uuid, userId), new("ClassId", NpgsqlDbType.Uuid, classId),
             .. Cursors(ErrorCursors, "dashboard_student_errors_")], ErrorCursors);
        using var tables = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió resultados.");
        return new StudentErrorsDashboardDto
        {
            Kpis = tables["Kpis"].AsEnumerable().Select(row => new StudentErrorsKpis
            {
                ClassId = Guid(row, "ClassId") ?? throw new DataException("Curso sin Id."),
                ModuleName = String(row, "ModuleName") ?? string.Empty,
                Year = Int(row, "Year"),
                Bimester = Int(row, "Bimester"),
                Parallel = String(row, "Parallel"),
                AnalyzedTexts = Long(row, "AnalyzedTexts"),
                TotalSentences = Long(row, "TotalSentences"),
                TotalErrors = Long(row, "TotalErrors"),
                SentencesWithErrors = Long(row, "SentencesWithErrors"),
                ErrorsPerText = Decimal(row, "ErrorsPerText"),
                AccuracyPercentage = Decimal(row, "AccuracyPercentage"),
                PreviousClassId = Guid(row, "PreviousClassId"),
                PreviousModuleName = String(row, "PreviousModuleName"),
                PreviousErrorsPerText = Decimal(row, "PreviousErrorsPerText"),
                PreviousAccuracyPercentage = Decimal(row, "PreviousAccuracyPercentage"),
                AccuracyChange = Decimal(row, "AccuracyChange")
            }).FirstOrDefault(),
            TopCategories = tables["TopCategories"].AsEnumerable().Select(row =>
            {
                var category = ReadCategory(row);
                category.PreviousErrorCount = LongOrNull(row, "PreviousErrorCount");
                return category;
            }).ToList()
        };
    }

    public async Task<PagedList<TextHistoryItem>> Texts(Guid userId, TextHistoryQuery query)
    {
        RequireUser(userId);
        RepositoryValidation.ValidateInput(query);
        if (query.ClassId == System.Guid.Empty) throw new ValidationException("ClassId no puede ser vacío.");
        RequireRange(query.FromDate, query.ToDate);
        return await Page("Text_History_List", "text_history_",
            [new("UserId", NpgsqlDbType.Uuid, userId), new("ClassId", NpgsqlDbType.Uuid, query.ClassId),
             new("FromDate", NpgsqlDbType.Date, query.FromDate), new("ToDate", NpgsqlDbType.Date, query.ToDate)],
            query, row => new TextHistoryItem
            {
                Id = Guid(row, "Id") ?? throw new DataException("Texto sin Id."),
                SubmittedAt = Date(row, "SubmittedAt") ?? throw new DataException("Texto sin fecha."),
                AnalyzedAt = Date(row, "AnalyzedAt"),
                Status = String(row, "Status") ?? string.Empty,
                ClassId = Guid(row, "ClassId"),
                ModuleName = String(row, "ModuleName"),
                Preview = String(row, "Preview") ?? string.Empty,
                WordCount = Int(row, "WordCount"),
                SentenceCount = Long(row, "SentenceCount"),
                ErrorCount = Long(row, "ErrorCount"),
                SentencesWithErrors = Long(row, "SentencesWithErrors"),
                AccuracyPercentage = Decimal(row, "AccuracyPercentage")
            });
    }

    public async Task<PagedList<FrequentErrorItem>> FrequentErrors(Guid userId, FrequentErrorQuery query)
    {
        RequireUser(userId);
        RepositoryValidation.ValidateInput(query);
        if (query.ClassId == System.Guid.Empty) throw new ValidationException("ClassId no puede ser vacío.");
        return await Page("Dashboard_StudentFrequentErrors_List", "dashboard_student_frequent_errors_",
            [new("UserId", NpgsqlDbType.Uuid, userId), new("ClassId", NpgsqlDbType.Uuid, query.ClassId)],
            query, row => new FrequentErrorItem
            {
                ErrorToken = String(row, "ErrorToken") ?? string.Empty,
                SuggestedCorrection = String(row, "SuggestedCorrection"),
                ErrorClassificationId = Guid(row, "ErrorClassificationId") ?? throw new DataException("Error sin categoría."),
                ErrorClassificationName = String(row, "ErrorClassificationName") ?? string.Empty,
                ErrorCount = Long(row, "ErrorCount"),
                RelatedTextCount = Long(row, "RelatedTextCount"),
                LastSeenAt = Date(row, "LastSeenAt")
            });
    }

    public async Task<PagedList<ChallengeProgressItem>> Challenges(Guid userId, ChallengeProgressQuery query)
    {
        RequireUser(userId);
        RepositoryValidation.ValidateInput(query);
        return await Page("UserChallenge_Progress_List", "userchallenge_progress_",
            [new("UserId", NpgsqlDbType.Uuid, userId), new("OnlyPending", NpgsqlDbType.Boolean, query.OnlyPending)],
            query, row => new ChallengeProgressItem
            {
                Id = Guid(row, "Id") ?? throw new DataException("Desafío sin Id."),
                Title = String(row, "Title") ?? string.Empty,
                Description = String(row, "Description"),
                Instructions = String(row, "Instructions"),
                Difficulty = String(row, "Difficulty") ?? string.Empty,
                Points = Int(row, "Points"),
                Round = Int(row, "Round"),
                StartDate = Date(row, "StartDate"),
                EndDate = Date(row, "EndDate"),
                IsCompleted = Bool(row, "IsCompleted"),
                CompletedAt = Date(row, "CompletedAt"),
                ConditionCount = Int(row, "ConditionCount"),
                ConditionsMet = Int(row, "ConditionsMet"),
                CanReach = Bool(row, "CanReach"),
                ProgressPercent = Decimal(row, "ProgressPercent"),
                Conditions = GamificationJson.ReadList<GamificationConditionProgress>(row, "Conditions")
            });
    }

    public async Task<PagedList<PointHistoryItem>> Points(Guid userId, PointHistoryQuery query)
    {
        RequireUser(userId);
        RepositoryValidation.ValidateInput(query);
        if (query.Source is not null && !PointHistoryQuery.Sources.Contains(query.Source, StringComparer.Ordinal))
            throw new ValidationException("Source debe ser Text, Evaluation, Achievement, Challenge, Course u Other.");
        RequireRange(query.FromDate, query.ToDate);
        return await Page("PointTransaction_History_List", "pointtransaction_history_",
            [new("UserId", NpgsqlDbType.Uuid, userId), new("Source", NpgsqlDbType.Text, query.Source),
             new("FromDate", NpgsqlDbType.Date, query.FromDate), new("ToDate", NpgsqlDbType.Date, query.ToDate)],
            query, row => new PointHistoryItem
            {
                Id = Guid(row, "Id") ?? throw new DataException("Movimiento sin Id."),
                Amount = Int(row, "Amount"),
                Reason = String(row, "Reason") ?? string.Empty,
                GrantedAt = Date(row, "GrantedAt") ?? throw new DataException("Movimiento sin fecha."),
                Source = String(row, "Source") ?? "Other",
                ModuleName = String(row, "ModuleName")
            });
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

    private static void RequireUser(Guid userId)
    {
        if (userId == System.Guid.Empty) throw new ValidationException("UserId debe identificar al usuario actual.");
    }

    private static void RequireRange(DateOnly? from, DateOnly? to)
    {
        if (from is not null && to is not null && to < from)
            throw new ValidationException("ToDate debe ser igual o posterior a FromDate.");
    }

    private static StudentDashboardKpis ReadKpis(DataRow row) => new()
    {
        FirstName = String(row, "FirstName") ?? string.Empty,
        LastName = String(row, "LastName") ?? string.Empty,
        MemberSince = Date(row, "MemberSince"),
        TotalPoints = Long(row, "TotalPoints"),
        LevelNumber = Int(row, "LevelNumber"),
        LevelName = String(row, "LevelName") ?? string.Empty,
        NextLevelNumber = IntOrNull(row, "NextLevelNumber"),
        NextLevelName = String(row, "NextLevelName"),
        NextLevelMinPoints = LongOrNull(row, "NextLevelMinPoints"),
        PointsToNextLevel = LongOrNull(row, "PointsToNextLevel"),
        LevelProgressPercent = Decimal(row, "LevelProgressPercent"),
        Rank = Long(row, "Rank"),
        RankedStudents = Long(row, "RankedStudents"),
        AnalyzedTexts = Long(row, "AnalyzedTexts"),
        TotalSentences = Long(row, "TotalSentences"),
        TotalErrors = Long(row, "TotalErrors"),
        SentencesWithErrors = Long(row, "SentencesWithErrors"),
        AccuracyPercentage = Decimal(row, "AccuracyPercentage"),
        ErrorsPerText = Decimal(row, "ErrorsPerText"),
        ErrorFreeTexts = Long(row, "ErrorFreeTexts"),
        AverageWords = Decimal(row, "AverageWords"),
        TextsThisWeek = Long(row, "TextsThisWeek"),
        TextsPreviousWeek = Long(row, "TextsPreviousWeek"),
        TextsThisMonth = Long(row, "TextsThisMonth"),
        LastTextAt = Date(row, "LastTextAt"),
        LastTextErrors = LongOrNull(row, "LastTextErrors"),
        BestTextErrors = LongOrNull(row, "BestTextErrors"),
        CurrentStreak = Long(row, "CurrentStreak"),
        BestStreak = Long(row, "BestStreak"),
        AchievementsObtained = Long(row, "AchievementsObtained"),
        ChallengesCompleted = Long(row, "ChallengesCompleted"),
        ScopePoints = Long(row, "ScopePoints"),
        PreviousClassId = Guid(row, "PreviousClassId"),
        PreviousModuleName = String(row, "PreviousModuleName"),
        PreviousTexts = LongOrNull(row, "PreviousTexts"),
        PreviousErrors = LongOrNull(row, "PreviousErrors"),
        PreviousAccuracy = Decimal(row, "PreviousAccuracy"),
        PreviousPoints = LongOrNull(row, "PreviousPoints")
    };

    private static StudentCourseErrors ReadCourse(DataRow row) => new()
    {
        ClassId = Guid(row, "ClassId") ?? throw new DataException("Curso sin Id."),
        ModuleName = String(row, "ModuleName") ?? string.Empty,
        ModuleLevel = Int(row, "ModuleLevel"),
        Year = Int(row, "Year"),
        Bimester = Int(row, "Bimester"),
        Parallel = String(row, "Parallel"),
        IsCurrent = Bool(row, "IsCurrent"),
        AnalyzedTexts = Long(row, "AnalyzedTexts"),
        TotalSentences = Long(row, "TotalSentences"),
        TotalErrors = Long(row, "TotalErrors"),
        ErrorsPerText = Decimal(row, "ErrorsPerText"),
        AccuracyPercentage = Decimal(row, "AccuracyPercentage")
    };

    private static DashboardErrorCategory ReadCategory(DataRow row) => new()
    {
        ErrorClassificationId = Guid(row, "ErrorClassificationId") ?? throw new DataException("Categoría sin Id."),
        ErrorClassificationName = String(row, "ErrorClassificationName") ?? string.Empty,
        ErrorCount = Long(row, "ErrorCount"),
        ErrorPercentage = Decimal(row, "ErrorPercentage"),
        RelatedTextCount = Long(row, "RelatedTextCount")
    };

    private static DashboardNextAchievement ReadAchievement(DataRow row) => new()
    {
        AchievementId = Guid(row, "AchievementId") ?? throw new DataException("Logro sin Id."),
        Name = String(row, "Name") ?? string.Empty,
        Description = String(row, "Description"),
        IconLink = String(row, "IconLink"),
        Points = Int(row, "Points"),
        ConditionCount = Int(row, "ConditionCount"),
        ConditionsMet = Int(row, "ConditionsMet"),
        ProgressPercent = Decimal(row, "ProgressPercent"),
        Conditions = GamificationJson.ReadList<GamificationConditionProgress>(row, "Conditions")
    };
}
