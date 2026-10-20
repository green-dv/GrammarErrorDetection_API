using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GrammarErrorDetection.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public sealed class UserController : ControllerBase
    {
        private readonly IUserService service;

        public UserController(IUserService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            this.service = service;
        }

        [HttpPost("search")]
        public async Task<Wrapper<List<UserListItemDto>>> List([FromBody] PaginationDto<UserList> request)
        {
            var data = await service.List(request);
            return new Wrapper<List<UserListItemDto>>
            {
                Data = data,
                Count = data.Count
            };
        }

        [HttpPost]
        public async Task<Wrapper<UserEntity>> Insert([FromBody] UserInsert request)
        {
            var data = await service.Insert(request);
            Response.StatusCode = StatusCodes.Status201Created;
            return new Wrapper<UserEntity> { Data = data };
        }

        [HttpPut]
        public async Task<Wrapper<UserEntity>> Update([FromBody] UserUpdate request)
        {
            var data = await service.Update(request);
            return new Wrapper<UserEntity> { Data = data };
        }
    }
}
