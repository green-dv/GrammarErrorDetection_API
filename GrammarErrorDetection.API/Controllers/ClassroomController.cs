using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classrooms;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GrammarErrorDetection.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public sealed class ClassroomController : ControllerBase
    {
        private readonly IClassroomService service;

        public ClassroomController(IClassroomService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            this.service = service;
        }

        [HttpGet]
        public async Task<Wrapper<List<ClassroomEntity>>> List([FromQuery] PaginationDto<ClassroomList> request)
        {
            var data = await service.List(request);
            return new Wrapper<List<ClassroomEntity>>
            {
                Data = data,
                Count = data.Count
            };
        }

        [HttpPost]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<ClassroomEntity>> Insert([FromBody] ClassroomInsert request)
        {
            var data = await service.Insert(request);
            Response.StatusCode = StatusCodes.Status201Created;
            return new Wrapper<ClassroomEntity> { Data = data };
        }

        [HttpPut]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<ClassroomEntity>> Update([FromBody] ClassroomUpdate request)
        {
            var data = await service.Update(request);
            return new Wrapper<ClassroomEntity> { Data = data };
        }
    }
}

