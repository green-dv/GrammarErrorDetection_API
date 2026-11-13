using System.Security.Claims;
using GrammarErrorDetection.Models.Grammar.Reports;
namespace GrammarErrorDetection.Services.Interfaces;
public interface IGrammarReportService
{
    Task<GrammarReportSummaryDto> Summary(ClaimsPrincipal user, GrammarReportQuery request);
    Task<List<GrammarReportCategoryDto>> Categories(ClaimsPrincipal user, GrammarReportQuery request);
    Task<List<GrammarReportComparisonDto>> Comparison(ClaimsPrincipal user, GrammarReportQuery request);
    Task<GrammarReportFile> Export(ClaimsPrincipal user, GrammarReportQuery request);
}
