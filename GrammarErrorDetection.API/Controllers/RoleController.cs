using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Roles;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class RoleController : ControllerBase
    {
        private readonly IRoleService service;

        public RoleController(IRoleService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            this.service = service;
        }

        [HttpGet]
        public async Task<Wrapper<List<RoleEntity>>> List([FromQuery] PaginationDto<RoleList> request)
        {
            var data = await service.List(request);
            return new Wrapper<List<RoleEntity>>
            {
                Data = data,
                Count = data.Count
            };
        }

        [HttpPost]
        public async Task<Wrapper<RoleEntity>> Insert([FromBody] RoleInsert request)
        {
            var data = await service.Insert(request);
            Response.StatusCode = StatusCodes.Status201Created;
            return new Wrapper<RoleEntity> { Data = data };
        }

        [HttpPut]
        public async Task<Wrapper<RoleEntity>> Update([FromBody] RoleUpdate request)
        {
            var data = await service.Update(request);
            return new Wrapper<RoleEntity> { Data = data };
        }
    }
}
