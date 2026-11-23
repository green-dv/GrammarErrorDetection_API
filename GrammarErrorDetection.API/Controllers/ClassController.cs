using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public sealed class ClassController : ControllerBase
    {
        private readonly IClassService service;

        public ClassController(IClassService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            this.service = service;
        }

        [HttpGet]
        public async Task<Wrapper<List<ClassEntity>>> List([FromQuery] PaginationDto<ClassList> request)
        {
            var page = await service.ListPage(request);
            return new() { Data = page.Items, Count = page.Items.Count, TotalCount = page.TotalCount };
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,Coordinator")]
        public async Task<Wrapper<ClassEntity>> Insert([FromBody] ClassInsert request)
        {
            // Un docente solo crea cursos a su cargo: se ignora el docente que envíe el cliente.
            if (!IsCoordinator) request.TeacherId = CurrentUserId();
            var data = await service.Insert(request);
            Response.StatusCode = StatusCodes.Status201Created;
            return new Wrapper<ClassEntity> { Data = data };
        }

        [HttpGet("eligible-students")]
        [Authorize(Roles = "Teacher,Coordinator")]
        public async Task<Wrapper<List<EligibleStudentDto>>> ListEligibleStudents([FromQuery] PaginationDto<EligibleStudentList> request)
        {
            var data = await service.ListEligibleStudents(request);
            return new Wrapper<List<EligibleStudentDto>> { Data = data, Count = data.Count };
        }

        [HttpPost("with-students")]
        [Authorize(Roles = "Teacher,Coordinator")]
        public async Task<Wrapper<ClassWithStudentsResult>> InsertWithStudents([FromBody] ClassWithStudentsInsert request)
        {
            if (!IsCoordinator) request.TeacherId = CurrentUserId();
            var data = await service.InsertWithStudents(request);
            Response.StatusCode = StatusCodes.Status201Created;
            return new Wrapper<ClassWithStudentsResult> { Data = data };
        }

        [HttpPut("with-students")]
        [Authorize(Roles = "Teacher,Coordinator")]
        public async Task<Wrapper<ClassWithStudentsResult>> UpdateWithStudents([FromBody] ClassWithStudentsUpdate request,
            [FromServices] ICourseAccessService access)
        {
            if (!IsCoordinator)
            {
                await access.EnsureOwnsClass(CurrentUserId(), request.Id);
                request.TeacherId = CurrentUserId();   // no puede traspasar el curso a otro docente
            }
            var data = await service.UpdateWithStudents(request);
            return new Wrapper<ClassWithStudentsResult> { Data = data };
        }

        // El token de invitación expira solo; no se persiste (ver CourseInvitationService).
        [HttpPost("{id:guid}/invitation")]
        [Authorize(Roles = "Coordinator,Teacher")]
        public async Task<Wrapper<CourseInvitationLinkDto>> CreateInvitation(
            [FromRoute, NotEmptyGuid] Guid id, [FromServices] ICourseInvitationService invitations)
        {
            var userId = CurrentUserId();
            var data = await invitations.CreateLink(userId, User.IsInRole("Coordinator"), id);
            return new Wrapper<CourseInvitationLinkDto> { Data = data };
        }

        [HttpPut]
        [Authorize(Roles = "Teacher,Coordinator")]
        public async Task<Wrapper<ClassEntity>> Update([FromBody] ClassUpdate request,
            [FromServices] ICourseAccessService access)
        {
            if (!IsCoordinator)
            {
                await access.EnsureOwnsClass(CurrentUserId(), request.Id);
                request.TeacherId = null;   // no puede traspasar el curso a otro docente
            }
            var data = await service.Update(request);
            return new Wrapper<ClassEntity> { Data = data };
        }

        private bool IsCoordinator => User.IsInRole("Coordinator");

        private Guid CurrentUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id != Guid.Empty
            ? id : throw new UnauthorizedAccessException();
    }
}

