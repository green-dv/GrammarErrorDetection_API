using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers;

[ApiController]
[Authorize(Roles = "Teacher,Coordinator")]
[Route("api/Class/{classId:guid}/registrations")]
public sealed class CourseRegistrationsController(IClassRepository classes, IRegistrationRepository registrations) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Wrapper<List<CourseRegistrationResult>>>> List(
        Guid classId,
        [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery, Range(1, 200)] int pageSize = 100)
    {
        if (classId == Guid.Empty) return BadRequest();
        var coordinator = User.IsInRole("Coordinator");
        Guid? teacherId = null;
        if (!coordinator)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
                return Forbid();
            teacherId = userId;
        }
        var course = (await classes.List(new PaginationDto<ClassList>
        {
            PageNumber = 1, PageSize = 1,
            Filter = new() { Id = classId, TeacherId = teacherId }
        })).SingleOrDefault();
        if (course is null || course.Id != classId || (!coordinator && course.TeacherId != teacherId))
            return Forbid();

        var data = await registrations.List(new PaginationDto<RegistrationList>
        {
            PageNumber = pageNumber, PageSize = pageSize,
            Filter = new() { ClassId = classId, Active = true }
        });
        // Solo se expone la asociación necesaria para las notas, sin facturas ni datos de otros cursos.
        var result = data.Where(item => item.ClassId == classId && item.Active == true)
            .Select(item => new CourseRegistrationResult(item.Id, item.UserId, classId, true)).ToList();
        return new Wrapper<List<CourseRegistrationResult>> { Data = result, Count = result.Count };
    }
}

public sealed record CourseRegistrationResult(Guid Id, Guid UserId, Guid ClassId, bool Active);
