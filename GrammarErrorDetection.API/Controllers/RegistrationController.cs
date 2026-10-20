using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public sealed class RegistrationController : ControllerBase
    {
        private readonly IRegistrationService service;

        public RegistrationController(IRegistrationService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            this.service = service;
        }

        [HttpGet]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<List<RegistrationListItemDto>>> List([FromQuery] PaginationDto<RegistrationSummaryList> request)
        {
            var data = await service.List(request);
            return new Wrapper<List<RegistrationListItemDto>>
            {
                Data = data,
                Count = data.Count
            };
        }

        [HttpGet("mine")]
        [Authorize(Roles = "Student")]
        public async Task<Wrapper<List<RegistrationListItemDto>>> ListMine([FromQuery] PaginationDto<RegistrationSummaryList> request)
        {
            var data = await service.ListMine(StudentId(), request);
            return new Wrapper<List<RegistrationListItemDto>> { Data = data, Count = data.Count };
        }

        [HttpGet("available-courses")]
        [Authorize(Roles = "Student")]
        public async Task<Wrapper<List<AvailableRegistrationCourseDto>>> ListAvailableCourses(
            [FromQuery] PaginationDto<AvailableRegistrationCourseList> request)
        {
            var data = await service.ListAvailableCourses(StudentId(), request);
            return new Wrapper<List<AvailableRegistrationCourseDto>> { Data = data, Count = data.Count };
        }

        [HttpGet("mine/courses")]
        [Authorize(Roles = "Student")]
        public async Task<Wrapper<List<StudentCourseHistoryDto>>> ListMyCourses([FromQuery] StudentCourseHistoryQuery request)
        {
            var data = await service.ListStudentCourses(StudentId(), request);
            return new Wrapper<List<StudentCourseHistoryDto>> { Data = data, Count = data.Count };
        }

        [HttpGet("students/{studentId:guid}/courses")]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<List<StudentCourseHistoryDto>>> ListStudentCourses(
            [FromRoute, NotEmptyGuid] Guid studentId, [FromQuery] StudentCourseHistoryQuery request)
        {
            var data = await service.ListStudentCourses(studentId, request);
            return new Wrapper<List<StudentCourseHistoryDto>> { Data = data, Count = data.Count };
        }

        [HttpPost("apply")]
        [Authorize(Roles = "Student")]
        public async Task<Wrapper<RegistrationEntity>> Submit([FromBody] StudentRegistrationRequest request)
        {
            var data = await service.Submit(StudentId(), request);
            return new Wrapper<RegistrationEntity> { Data = data };
        }

        [HttpPut("{id:guid}/decision")]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<RegistrationEntity>> Decide([FromRoute, NotEmptyGuid] Guid id,
            [FromBody] RegistrationDecisionRequest request)
        {
            var data = await service.Decide(id, request);
            return new Wrapper<RegistrationEntity> { Data = data };
        }

        private Guid StudentId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            && id != Guid.Empty ? id : throw new UnauthorizedAccessException();

        [HttpPost]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<RegistrationEntity>> Insert([FromBody] RegistrationInsert request)
        {
            var data = await service.Insert(request);
            Response.StatusCode = StatusCodes.Status201Created;
            return new Wrapper<RegistrationEntity> { Data = data };
        }

        [HttpPut]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<RegistrationEntity>> Update([FromBody] RegistrationUpdate request)
        {
            var data = await service.Update(request);
            return new Wrapper<RegistrationEntity> { Data = data };
        }

        [HttpPost("generate-next-bimester")]
        [Authorize(Roles = "Coordinator")]
        public async Task<ActionResult<Wrapper<RegistrationGenerationResult>>> GenerateNextBimester(
            [FromBody] RegistrationGenerationRequest request)
        {
            var data = await service.GenerateNextBimester(request);
            return Ok(new Wrapper<RegistrationGenerationResult> { Data = data });
        }
    }
}

