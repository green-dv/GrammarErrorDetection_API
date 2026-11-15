namespace GrammarErrorDetection.Models.Grammar.Reports;

public sealed class GrammarReportCategoryDto
{
    public Guid ErrorClassificationId { get; set; }
    public string ErrorClassificationName { get; set; } = string.Empty;
    public long ErrorCount { get; set; }
    public long RelatedTextCount { get; set; }
    public long RelatedSentenceCount { get; set; }
    public long AffectedStudentCount { get; set; }
    public long EnrolledStudentCount { get; set; }
    public long StudentsWithAnalyzedTexts { get; set; }
    public decimal? AffectedStudentPercentage { get; set; }
    public decimal? AffectedAnalyzedStudentPercentage { get; set; }
    public decimal? ErrorPercentage { get; set; }
    public decimal? ErrorsPerAnalyzedText { get; set; }
}
