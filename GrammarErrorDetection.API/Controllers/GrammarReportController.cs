using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Reports;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize(Roles = "Student,Teacher,Coordinator")]
[Route("api/[controller]")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GrammarReportController(IGrammarReportService service) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<Wrapper<GrammarReportSummaryDto>> Summary([FromQuery] GrammarReportQuery request)
        => new() { Data = await service.Summary(User, request), Count = 1 };

    [HttpGet("categories")]
    public async Task<Wrapper<List<GrammarReportCategoryDto>>> Categories([FromQuery] GrammarReportQuery request)
    {
        var rows = await service.Categories(User, request);
        return new() { Data = rows, Count = rows.Count };
    }

    [HttpGet("comparison")]
    public async Task<Wrapper<List<GrammarReportComparisonDto>>> Comparison([FromQuery] GrammarReportQuery request)
    {
        var rows = await service.Comparison(User, request);
        return new() { Data = rows, Count = rows.Count };
    }

    [HttpGet("export")]
    [HttpGet("export/affected")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<FileContentResult> Export([FromQuery] GrammarReportQuery request)
    {
        var report = await service.Export(User, request);
        return File(report.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", report.FileName);
    }
}
