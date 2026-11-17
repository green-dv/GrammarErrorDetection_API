using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

/// <summary>Dashboards por rol. El usuario se toma siempre del JWT.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController(IStudentDashboardService students, IStaffDashboardService staff) : ControllerBase
{
    [HttpGet("teacher")]
    [Authorize(Roles = "Teacher")]
    public async Task<Wrapper<TeacherDashboardDto>> Teacher([FromQuery] TeacherDashboardQuery query)
        => new() { Data = await staff.Teacher(CurrentUserId(), query) };

    [HttpGet("teacher/at-risk")]
    [Authorize(Roles = "Teacher,Coordinator")]
    public async Task<Wrapper<List<AtRiskStudent>>> AtRisk([FromQuery] AtRiskQuery query)
        => Page(await staff.AtRisk(CurrentUserId(), User.IsInRole("Coordinator"), query));

    [HttpGet("coordinator")]
    [Authorize(Roles = "Coordinator")]
    public async Task<Wrapper<CoordinatorDashboardDto>> Coordinator([FromQuery] CoordinatorPeriodQuery query)
        => new() { Data = await staff.Coordinator(query) };

    [HttpGet("coordinator/classes")]
    [Authorize(Roles = "Coordinator")]
    public async Task<Wrapper<List<ClassCoverageItem>>> ClassCoverage([FromQuery] ClassCoverageQuery query)
        => Page(await staff.ClassCoverage(query));

    [HttpGet("coordinator/teachers")]
    [Authorize(Roles = "Coordinator")]
    public async Task<Wrapper<List<TeacherRankingItem>>> TeacherRanking([FromQuery] TeacherRankingQuery query)
        => Page(await staff.TeacherRanking(query));

    [HttpGet("student")]
    [Authorize(Roles = "Student")]
    public async Task<Wrapper<StudentDashboardDto>> Student([FromQuery] StudentDashboardQuery query)
        => new() { Data = await students.Summary(CurrentUserId(), query) };

    [HttpGet("student/frequent-errors")]
    [Authorize(Roles = "Student")]
    public async Task<Wrapper<List<FrequentErrorItem>>> StudentFrequentErrors([FromQuery] FrequentErrorQuery query)
        => Page(await students.FrequentErrors(CurrentUserId(), query));

    [HttpGet("student/errors")]
    [Authorize(Roles = "Student")]
    public async Task<Wrapper<StudentErrorsDashboardDto>> StudentErrors([FromQuery] StudentErrorsQuery query)
        => new() { Data = await students.Errors(CurrentUserId(), query) };

    [HttpGet("student/texts")]
    [Authorize(Roles = "Student")]
    public async Task<Wrapper<List<TextHistoryItem>>> StudentTexts([FromQuery] TextHistoryQuery query)
        => Page(await students.Texts(CurrentUserId(), query));

    [HttpGet("student/challenges")]
    [Authorize(Roles = "Student")]
    public async Task<Wrapper<List<ChallengeProgressItem>>> StudentChallenges([FromQuery] ChallengeProgressQuery query)
        => Page(await students.Challenges(CurrentUserId(), query));

    [HttpGet("student/points")]
    [Authorize(Roles = "Student")]
    public async Task<Wrapper<List<PointHistoryItem>>> StudentPoints([FromQuery] PointHistoryQuery query)
        => Page(await students.Points(CurrentUserId(), query));

    private static Wrapper<List<T>> Page<T>(PagedList<T> page)
        => new() { Data = page.Items, Count = page.Items.Count, TotalCount = page.TotalCount };

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id != Guid.Empty
            ? id
            : throw new UnauthorizedAccessException("El token no identifica al usuario.");
}
