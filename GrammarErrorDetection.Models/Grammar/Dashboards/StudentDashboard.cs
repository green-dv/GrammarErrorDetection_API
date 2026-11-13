using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Models.Grammar.Dashboards;

/// <summary>Página de un listado con el formato de dos DataTables.</summary>
public sealed record PagedList<T>(List<T> Items, long TotalCount);

/// <summary>Resumen del dashboard del estudiante (Dashboard_Student): datos livianos sin paginar.</summary>
public sealed class StudentDashboardDto
{
    public StudentDashboardKpis? Kpis { get; set; }
    public List<StudentCourseErrors> ErrorsByCourse { get; set; } = [];
    public List<DashboardErrorCategory> TopCategories { get; set; } = [];
    public List<DashboardNextAchievement> NextAchievements { get; set; } = [];
    public List<DashboardDailyActivity> DailyActivity { get; set; } = [];
}

/// <summary>Filtro del resumen: un curso aprobado del estudiante o null para todo su historial.</summary>
public sealed class StudentDashboardQuery
{
    public Guid? ClassId { get; set; }
}

public sealed class StudentDashboardKpis
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? MemberSince { get; set; }
    public long TotalPoints { get; set; }
    public int LevelNumber { get; set; }
    public string LevelName { get; set; } = string.Empty;
    public int? NextLevelNumber { get; set; }
    public string? NextLevelName { get; set; }
    public long? NextLevelMinPoints { get; set; }
    public long? PointsToNextLevel { get; set; }
    public decimal? LevelProgressPercent { get; set; }
    public long Rank { get; set; }
    public long RankedStudents { get; set; }
    public long AnalyzedTexts { get; set; }
    public long TotalSentences { get; set; }
    public long TotalErrors { get; set; }
    public long SentencesWithErrors { get; set; }
    public decimal? AccuracyPercentage { get; set; }
    public decimal? ErrorsPerText { get; set; }
    public long ErrorFreeTexts { get; set; }
    public decimal? AverageWords { get; set; }
    public long TextsThisWeek { get; set; }
    public long TextsPreviousWeek { get; set; }
    public long TextsThisMonth { get; set; }
    public DateTime? LastTextAt { get; set; }
    public long? LastTextErrors { get; set; }
    public long? BestTextErrors { get; set; }
    public long CurrentStreak { get; set; }
    public long BestStreak { get; set; }
    public long AchievementsObtained { get; set; }
    public long ChallengesCompleted { get; set; }
    /// <summary>Puntos ganados en el alcance (curso elegido o todo el historial).</summary>
    public long ScopePoints { get; set; }
    // Solo con un curso elegido: el curso aprobado anterior, para las tendencias. NULL sin curso o sin anterior.
    public Guid? PreviousClassId { get; set; }
    public string? PreviousModuleName { get; set; }
    public long? PreviousTexts { get; set; }
    public long? PreviousErrors { get; set; }
    public decimal? PreviousAccuracy { get; set; }
    public long? PreviousPoints { get; set; }
}

public sealed class StudentCourseErrors
{
    public Guid ClassId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int ModuleLevel { get; set; }
    public int Year { get; set; }
    public int Bimester { get; set; }
    public string? Parallel { get; set; }
    public bool IsCurrent { get; set; }
    public long AnalyzedTexts { get; set; }
    public long TotalSentences { get; set; }
    public long TotalErrors { get; set; }
    public decimal? ErrorsPerText { get; set; }
    public decimal? AccuracyPercentage { get; set; }
}

public sealed class DashboardErrorCategory
{
    public Guid ErrorClassificationId { get; set; }
    public string ErrorClassificationName { get; set; } = string.Empty;
    public long ErrorCount { get; set; }
    public decimal? ErrorPercentage { get; set; }
    public long RelatedTextCount { get; set; }
    /// <summary>Solo en el mini dashboard de errores: errores de la categoría en el curso anterior.</summary>
    public long? PreviousErrorCount { get; set; }
    /// <summary>Solo en el resumen: errores promedio por estudiante de los compañeros del curso.</summary>
    public decimal? CourseAverage { get; set; }
}

public sealed class DashboardNextAchievement
{
    public Guid AchievementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconLink { get; set; }
    public int Points { get; set; }
    public int ConditionCount { get; set; }
    public int ConditionsMet { get; set; }
    public decimal? ProgressPercent { get; set; }
    public List<GamificationConditionProgress> Conditions { get; set; } = [];
}

public sealed class DashboardDailyActivity
{
    public DateTime Day { get; set; }
    public long AnalyzedTexts { get; set; }
    public long TotalErrors { get; set; }
}

/// <summary>Mini dashboard de errores de un curso (Dashboard_StudentErrors).</summary>
public sealed class StudentErrorsDashboardDto
{
    public StudentErrorsKpis? Kpis { get; set; }
    public List<DashboardErrorCategory> TopCategories { get; set; } = [];
}

public sealed class StudentErrorsKpis
{
    public Guid ClassId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Bimester { get; set; }
    public string? Parallel { get; set; }
    public long AnalyzedTexts { get; set; }
    public long TotalSentences { get; set; }
    public long TotalErrors { get; set; }
    public long SentencesWithErrors { get; set; }
    public decimal? ErrorsPerText { get; set; }
    public decimal? AccuracyPercentage { get; set; }
    public Guid? PreviousClassId { get; set; }
    public string? PreviousModuleName { get; set; }
    public decimal? PreviousErrorsPerText { get; set; }
    public decimal? PreviousAccuracyPercentage { get; set; }
    public decimal? AccuracyChange { get; set; }
}

public sealed class StudentErrorsQuery
{
    public Guid? ClassId { get; set; }
}

public abstract class DashboardPageQuery
{
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber debe ser mayor que cero.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize debe estar entre 1 y 100.")]
    public int PageSize { get; set; } = 10;
}

public sealed class TextHistoryQuery : DashboardPageQuery
{
    public Guid? ClassId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public sealed class TextHistoryItem
{
    public Guid Id { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? AnalyzedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ClassId { get; set; }
    public string? ModuleName { get; set; }
    public string Preview { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public long SentenceCount { get; set; }
    public long ErrorCount { get; set; }
    public long SentencesWithErrors { get; set; }
    public decimal? AccuracyPercentage { get; set; }
}

public sealed class FrequentErrorQuery : DashboardPageQuery
{
    public Guid? ClassId { get; set; }
}

/// <summary>Error concreto repetido: palabra marcada y corrección sugerida.</summary>
public sealed class FrequentErrorItem
{
    public string ErrorToken { get; set; } = string.Empty;
    public string? SuggestedCorrection { get; set; }
    public Guid ErrorClassificationId { get; set; }
    public string ErrorClassificationName { get; set; } = string.Empty;
    public long ErrorCount { get; set; }
    public long RelatedTextCount { get; set; }
    public DateTime? LastSeenAt { get; set; }
}

public sealed class ChallengeProgressQuery : DashboardPageQuery
{
    public bool? OnlyPending { get; set; }
}

public sealed class ChallengeProgressItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Round { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ConditionCount { get; set; }
    public int ConditionsMet { get; set; }
    public bool CanReach { get; set; }
    public decimal? ProgressPercent { get; set; }
    public List<GamificationConditionProgress> Conditions { get; set; } = [];
}

public sealed class PointHistoryQuery : DashboardPageQuery
{
    public static readonly string[] Sources = ["Text", "Evaluation", "Achievement", "Challenge", "Course", "Other"];

    public string? Source { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public sealed class PointHistoryItem
{
    public Guid Id { get; set; }
    public int Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? ModuleName { get; set; }
}
