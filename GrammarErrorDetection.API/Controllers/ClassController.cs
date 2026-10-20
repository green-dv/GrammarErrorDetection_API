using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
            var data = await service.List(request);
            return new Wrapper<List<ClassEntity>>
            {
                Data = data,
                Count = data.Count
            };
        }

        [HttpPost]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<ClassEntity>> Insert([FromBody] ClassInsert request)
        {
            var data = await service.Insert(request);
            Response.StatusCode = StatusCodes.Status201Created;
            return new Wrapper<ClassEntity> { Data = data };
        }

        [HttpGet("eligible-students")]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<List<EligibleStudentDto>>> ListEligibleStudents([FromQuery] PaginationDto<EligibleStudentList> request)
        {
            var data = await service.ListEligibleStudents(request);
            return new Wrapper<List<EligibleStudentDto>> { Data = data, Count = data.Count };
        }

        [HttpPost("with-students")]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<ClassWithStudentsResult>> InsertWithStudents([FromBody] ClassWithStudentsInsert request)
        {
            var data = await service.InsertWithStudents(request);
            Response.StatusCode = StatusCodes.Status201Created;
            return new Wrapper<ClassWithStudentsResult> { Data = data };
        }

        [HttpPut("with-students")]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<ClassWithStudentsResult>> UpdateWithStudents([FromBody] ClassWithStudentsUpdate request)
        {
            var data = await service.UpdateWithStudents(request);
            return new Wrapper<ClassWithStudentsResult> { Data = data };
        }

        [HttpPut]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<ClassEntity>> Update([FromBody] ClassUpdate request)
        {
            var data = await service.Update(request);
            return new Wrapper<ClassEntity> { Data = data };
        }
    }
}

