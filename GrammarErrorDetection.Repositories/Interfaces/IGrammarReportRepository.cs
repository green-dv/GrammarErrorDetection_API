using GrammarErrorDetection.Models.Grammar.Reports;
namespace GrammarErrorDetection.Repositories.Interfaces;
public interface IGrammarReportRepository
{
    Task<GrammarReportSummaryDto> Summary(GrammarReportQuery request);
    Task<List<GrammarReportCategoryDto>> Categories(GrammarReportQuery request);
    Task<List<GrammarReportComparisonDto>> Comparison(GrammarReportQuery request);
}
