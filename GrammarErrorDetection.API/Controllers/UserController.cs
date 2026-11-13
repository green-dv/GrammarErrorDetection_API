using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GrammarErrorDetection.API.Controllers
{
    /// <summary>
    /// Usuarios. El alcance de lectura y edición lo decide <see cref="IUserAccessService"/> con el usuario del JWT.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public sealed class UserController : ControllerBase
    {
        private readonly IUserAccessService access;
        private readonly IUserProvisioningService provisioning;

        public UserController(IUserAccessService access, IUserProvisioningService provisioning)
        {
            ArgumentNullException.ThrowIfNull(access);
            ArgumentNullException.ThrowIfNull(provisioning);
            this.access = access;
            this.provisioning = provisioning;
        }

        [HttpGet("{id:guid}")]
        public async Task<Wrapper<UserListItemDto>> Get(Guid id, CancellationToken cancellationToken)
        {
            var data = await access.GetAsync(Actor(), id, cancellationToken);
            return new Wrapper<UserListItemDto> { Data = data };
        }

        [HttpPost("search")]
        [Authorize(Roles = "Teacher,Coordinator")]
        public async Task<Wrapper<List<UserListItemDto>>> List([FromBody] PaginationDto<UserList> request)
        {
            var data = await access.SearchAsync(Actor(), request);
            return new Wrapper<List<UserListItemDto>>
            {
                Data = data,
                Count = data.Count
            };
        }

        /// <summary>Alta por el coordinador con rol definido; la contraseña temporal se envía al correo del usuario.</summary>
        [HttpPost]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<UserCredentialsResult>> Insert([FromBody] UserCreateRequest request)
        {
            var data = await provisioning.CreateAsync(request);
            Response.StatusCode = StatusCodes.Status201Created;
            return new Wrapper<UserCredentialsResult> { Data = data };
        }

        /// <summary>Genera una nueva contraseña temporal, exige cambiarla y la envía por correo.</summary>
        [HttpPost("{id:guid}/resend-credentials")]
        [Authorize(Roles = "Coordinator")]
        public async Task<Wrapper<UserCredentialsResult>> ResendCredentials(Guid id)
        {
            var data = await provisioning.ResendCredentialsAsync(Actor().Id, id);
            return new Wrapper<UserCredentialsResult> { Data = data };
        }

        [HttpPut]
        public async Task<Wrapper<UserEntity>> Update([FromBody] UserUpdate request)
        {
            var data = await access.UpdateAsync(Actor(), request);
            return new Wrapper<UserEntity> { Data = data };
        }

        private UserActor Actor()
            => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id != Guid.Empty
                ? new UserActor(id, User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray())
                : throw new UnauthorizedAccessException("El token no identifica al usuario.");
    }
}
