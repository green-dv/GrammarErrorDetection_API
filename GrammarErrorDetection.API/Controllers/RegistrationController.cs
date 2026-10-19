using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
        public async Task<Wrapper<List<RegistrationEntity>>> List([FromQuery] PaginationDto<RegistrationList> request)
        {
            var data = await service.List(request);
            return new Wrapper<List<RegistrationEntity>>
            {
                Data = data,
                Count = data.Count
            };
        }

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

