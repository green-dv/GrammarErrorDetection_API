using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.VerificationCodes;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GrammarErrorDetection.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Coordinator")]
    [Route("api/[controller]")]
    public sealed class VerificationCodeController : ControllerBase
    {
        private readonly IVerificationCodeService service;

        public VerificationCodeController(IVerificationCodeService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            this.service = service;
        }

        [HttpGet]
        public async Task<Wrapper<List<VerificationCodeEntity>>> List([FromQuery] PaginationDto<VerificationCodeList> request)
        {
            var data = await service.List(request);
            return new Wrapper<List<VerificationCodeEntity>>
            {
                Data = data,
                Count = data.Count
            };
        }

        [HttpPost]
        public async Task<Wrapper<VerificationCodeEntity>> Insert([FromBody] VerificationCodeInsert request)
        {
            var data = await service.Insert(request);
            Response.StatusCode = StatusCodes.Status201Created;
            return new Wrapper<VerificationCodeEntity> { Data = data };
        }

        [HttpPut]
        public async Task<Wrapper<VerificationCodeEntity>> Update([FromBody] VerificationCodeUpdate request)
        {
            var data = await service.Update(request);
            return new Wrapper<VerificationCodeEntity> { Data = data };
        }
    }
}
