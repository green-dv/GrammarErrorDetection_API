using GrammarErrorDetection.Models.Grammar.Dashboards;
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

        private readonly ILogger<RegistrationController> logger;

        public RegistrationController(IRegistrationService service, ILogger<RegistrationController> logger)
        {
            ArgumentNullException.ThrowIfNull(service);
            this.service = service;
            this.logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<List<RegistrationListItemDto>>> List([FromQuery] PaginationDto<RegistrationSummaryList> request)
        {
            var page = await service.ListPage(request);
            return new Wrapper<List<RegistrationListItemDto>>
            {
                Data = page.Items,
                Count = page.Items.Count,
                TotalCount = page.TotalCount
            };
        }

        [HttpGet("mine")]
        [Authorize(Roles = "Student")]
        public async Task<Wrapper<List<RegistrationListItemDto>>> ListMine([FromQuery] PaginationDto<RegistrationSummaryList> request)
        {
            var page = await service.ListMinePage(StudentId(), request);
            return new Wrapper<List<RegistrationListItemDto>> { Data = page.Items, Count = page.Items.Count, TotalCount = page.TotalCount };
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
            var page = await service.ListStudentCoursesPage(StudentId(), request);
            return new Wrapper<List<StudentCourseHistoryDto>> { Data = page.Items, Count = page.Items.Count, TotalCount = page.TotalCount };
        }

        [HttpGet("students/{studentId:guid}/courses")]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<List<StudentCourseHistoryDto>>> ListStudentCourses(
            [FromRoute, NotEmptyGuid] Guid studentId, [FromQuery] StudentCourseHistoryQuery request)
        {
            var page = await service.ListStudentCoursesPage(studentId, request);
            return new Wrapper<List<StudentCourseHistoryDto>> { Data = page.Items, Count = page.Items.Count, TotalCount = page.TotalCount };
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
            logger.LogInformation("Generación {Year}-{Bimester}: {Created} cursos creados, {Reused} reutilizados, {Registrations} inscripciones, {Skipped} omitidas",
                request.Year, request.Bimester, data.CreatedClassIds.Count, data.ReusedClassIds.Count, data.CreatedRegistrationIds.Count, data.Skipped.Count);
            return Ok(new Wrapper<RegistrationGenerationResult> { Data = data });
        }
    }
}

