namespace GrammarErrorDetection.Models.Grammar.Reports;

public sealed class GrammarReportSummaryDto
{
    public Guid? ClassId { get; set; }
    public Guid? UserId { get; set; }
    public long EnrolledStudentCount { get; set; }
    public long StudentsWithAnalyzedTexts { get; set; }
    public long TotalTexts { get; set; }
    public long AnalyzedTexts { get; set; }
    public long TotalSentences { get; set; }
    public long TotalErrors { get; set; }
    public decimal? StudentCoveragePercentage { get; set; }
    public decimal? ErrorsPerTextTotal { get; set; }
    public decimal? ErrorsPerAnalyzedText { get; set; }
    // Oraciones analizadas con al menos un error y precisión (% de oraciones sin errores; null sin oraciones).
    public long SentencesWithErrors { get; set; }
    public decimal? AccuracyPercentage { get; set; }
}
