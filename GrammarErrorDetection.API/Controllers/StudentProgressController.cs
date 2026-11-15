using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Reports;
using GrammarErrorDetection.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize(Roles = "Student,Teacher,Coordinator")]
[Route("api/[controller]")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class StudentProgressController(IStudentProgressRepository progress, IClassRepository classes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Wrapper<List<StudentProgressDto>>>> List([FromQuery] StudentProgressQuery request)
    {
        if (request.ClassId is null && request.UserId is null) return BadRequest();

        var userId = request.UserId;
        if (!User.IsInRole("Coordinator"))
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) || actor == Guid.Empty)
                return Forbid();

            if (User.IsInRole("Teacher"))
            {
                if (request.ClassId is not Guid classId) return Forbid();
                var courses = await classes.List(new PaginationDto<ClassList>
                {
                    PageNumber = 1,
                    PageSize = 1,
                    Filter = new() { Id = classId, TeacherId = actor }
                });
                if (!courses.Any(course => course.Id == classId && course.TeacherId == actor)) return Forbid();
            }
            else
            {
                if (!User.IsInRole("Student") || (userId.HasValue && userId != actor)) return Forbid();
                userId = actor;
            }
        }

        var rows = await progress.List(request.ClassId, userId);
        return new Wrapper<List<StudentProgressDto>> { Data = rows, Count = rows.Count };
    }
}
