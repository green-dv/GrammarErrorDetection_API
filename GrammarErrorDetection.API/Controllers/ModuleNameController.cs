using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.ModuleNames;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GrammarErrorDetection.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public sealed class ModuleNameController : ControllerBase
    {
        private readonly IModuleNameService service;

        public ModuleNameController(IModuleNameService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            this.service = service;
        }

        [HttpGet]
        public async Task<Wrapper<List<ModuleNameEntity>>> List([FromQuery] PaginationDto<ModuleNameList> request)
        {
            var data = await service.List(request);
            return new Wrapper<List<ModuleNameEntity>>
            {
                Data = data,
                Count = data.Count
            };
        }

        [HttpPost]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<ModuleNameEntity>> Insert([FromBody] ModuleNameInsert request)
        {
            var data = await service.Insert(request);
            Response.StatusCode = StatusCodes.Status201Created;
            return new Wrapper<ModuleNameEntity> { Data = data };
        }

        [HttpPut]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<ModuleNameEntity>> Update([FromBody] ModuleNameUpdate request)
        {
            var data = await service.Update(request);
            return new Wrapper<ModuleNameEntity> { Data = data };
        }
    }
}

