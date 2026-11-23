using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GrammarErrorDetection.Models.Grammar.Validation;
using System.ComponentModel.DataAnnotations;

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
        [Authorize(Roles = "Teacher,Coordinator")]
        public async Task<Wrapper<List<RegistrationListItemDto>>> List([FromQuery] PaginationDto<RegistrationSummaryList> request,
            [FromServices] ICourseAccessService access)
        {
            var page = IsCoordinator ? await service.ListPage(request) : await ListOwned(request, access);
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

        // Vista previa de la invitación: mismas validaciones que Join, pero sin inscribir.
        // Docentes y coordinadores solo reciben los datos del curso (la pantalla les explica que su rol no permite inscribirse).
        [HttpPost("join/preview")]
        [Authorize(Roles = "Student,Teacher,Coordinator")]
        public async Task<Wrapper<CourseJoinResultDto>> JoinPreview([FromBody] CourseJoinRequest request,
            [FromServices] ICourseInvitationService invitations)
        {
            var data = User.IsInRole("Student")
                ? await invitations.Preview(StudentId(), request.Token)
                : await invitations.Describe(request.Token);
            return new Wrapper<CourseJoinResultDto> { Data = data };
        }

        // Inscripción por enlace/QR de invitación: sin requisito del curso anterior, aprobada al instante.
        [HttpPost("join")]
        [Authorize(Roles = "Student")]
        public async Task<Wrapper<CourseJoinResultDto>> Join([FromBody] CourseJoinRequest request,
            [FromServices] ICourseInvitationService invitations)
        {
            var data = await invitations.Join(StudentId(), request.Token);
            return new Wrapper<CourseJoinResultDto> { Data = data };
        }

        [HttpPut("{id:guid}/decision")]
        [Authorize(Roles = "Teacher,Coordinator")]
        public async Task<Wrapper<RegistrationEntity>> Decide([FromRoute, NotEmptyGuid] Guid id,
            [FromBody] RegistrationDecisionRequest request, [FromServices] ICourseAccessService access)
        {
            if (!IsCoordinator) await access.EnsureOwnsRegistration(StudentId(), id);
            var data = await service.Decide(id, request);
            return new Wrapper<RegistrationEntity> { Data = data };
        }

        private bool IsCoordinator => User.IsInRole("Coordinator");

        // Las inscripciones se listan por usuario/módulo, no por curso: se leen todas las páginas y se filtran
        // por los cursos del docente antes de paginar.
        private async Task<PagedList<RegistrationListItemDto>> ListOwned(PaginationDto<RegistrationSummaryList> request,
            ICourseAccessService access)
        {
            var owned = await access.OwnedClassIds(StudentId());
            var matches = new List<RegistrationListItemDto>();
            const int batchSize = 200;
            for (var page = 1; ; page++)
            {
                var batch = await service.ListPage(new PaginationDto<RegistrationSummaryList>
                { PageNumber = page, PageSize = batchSize, Filter = request.Filter });
                matches.AddRange(batch.Items.Where(r => r.ClassId is Guid c && owned.Contains(c)));
                if (batch.Items.Count < batchSize) break;
            }
            var size = request.PageSize > 0 ? request.PageSize : matches.Count;
            var items = matches.Skip((Math.Max(request.PageNumber, 1) - 1) * size).Take(size).ToList();
            return new PagedList<RegistrationListItemDto>(items, matches.Count);
        }

        private Guid StudentId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            && id != Guid.Empty ? id : throw new UnauthorizedAccessException();

        [HttpPost]
        [Authorize(Roles = "Teacher,Coordinator")]
        public async Task<Wrapper<RegistrationEntity>> Insert([FromBody] RegistrationInsert request,
            [FromServices] ICourseAccessService access)
        {
            if (!IsCoordinator)
                await access.EnsureOwnsClass(StudentId(), request.ClassId ?? throw new ValidationException("Indica el curso de la inscripción."));
            var data = await service.Insert(request);
            Response.StatusCode = StatusCodes.Status201Created;
            return new Wrapper<RegistrationEntity> { Data = data };
        }

        [HttpPut]
        [Authorize(Roles = "Teacher,Coordinator")]
        public async Task<Wrapper<RegistrationEntity>> Update([FromBody] RegistrationUpdate request,
            [FromServices] ICourseAccessService access)
        {
            if (!IsCoordinator)
            {
                await access.EnsureOwnsRegistration(StudentId(), request.Id);
                if (request.ClassId is Guid target) await access.EnsureOwnsClass(StudentId(), target);   // tampoco la mueve a un curso ajeno
            }
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

