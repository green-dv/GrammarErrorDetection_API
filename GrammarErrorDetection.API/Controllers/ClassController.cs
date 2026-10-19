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

        [HttpPut]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<ClassEntity>> Update([FromBody] ClassUpdate request)
        {
            var data = await service.Update(request);
            return new Wrapper<ClassEntity> { Data = data };
        }
    }
}

