namespace GrammarErrorDetection.Models.Grammar.Reports;

public sealed class GrammarReportComparisonDto
{
    public string Scope { get; set; } = string.Empty;
    public string ComparisonStatus { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty;
    public Guid ClassId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ErrorClassificationId { get; set; }
    public string? ErrorClassificationName { get; set; }
    public long CurrentEnrolledStudentCount { get; set; }
    public long PreviousEnrolledStudentCount { get; set; }
    public Guid? PreviousClassId { get; set; }
    public Guid[] PreviousClassIds { get; set; } = [];
    public long StudentsWithPreviousClassCount { get; set; }
    public long StudentsWithoutPreviousClassCount { get; set; }
    public long CurrentTotalTexts { get; set; }
    public long CurrentAnalyzedTexts { get; set; }
    public long CurrentStudentsWithAnalyzedTexts { get; set; }
    public long CurrentErrorCount { get; set; }
    public long CurrentRelatedTextCount { get; set; }
    public long CurrentAffectedStudentCount { get; set; }
    public long PreviousTotalTexts { get; set; }
    public long PreviousAnalyzedTexts { get; set; }
    public long PreviousStudentsWithAnalyzedTexts { get; set; }
    public long PreviousErrorCount { get; set; }
    public long PreviousRelatedTextCount { get; set; }
    public long PreviousAffectedStudentCount { get; set; }
    public long? ErrorCountChange { get; set; }
    public long? AffectedStudentCountChange { get; set; }
    public decimal? AffectedStudentPercentagePointChange { get; set; }
    public decimal? AffectedAnalyzedStudentPercentagePointChange { get; set; }
    public decimal? ErrorsPerAnalyzedTextChange { get; set; }
    public decimal? ErrorCountChangePercentage { get; set; }
    public decimal? ErrorsPerAnalyzedTextChangePercentage { get; set; }
    public decimal? CurrentAffectedStudentPercentage { get; set; }
    public decimal? CurrentAffectedAnalyzedStudentPercentage { get; set; }
    public decimal? CurrentErrorsPerAnalyzedText { get; set; }
    public decimal? PreviousAffectedStudentPercentage { get; set; }
    public decimal? PreviousAffectedAnalyzedStudentPercentage { get; set; }
    public decimal? PreviousErrorsPerAnalyzedText { get; set; }
}
