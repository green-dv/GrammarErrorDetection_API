using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Reports;

public sealed class GrammarReportQuery
{
    [NotEmptyGuid] public Guid? ClassId { get; set; }
    [NotEmptyGuid] public Guid? UserId { get; set; }
    public string? ReportScope { get; set; }
}

public sealed record GrammarReportData(GrammarReportSummaryDto Summary,
    List<GrammarReportCategoryDto> Categories, List<GrammarReportComparisonDto> Comparison);

public sealed record GrammarReportFile(byte[] Content, string FileName);
