using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrammarErrorDetection.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IAuthService service;

        public AuthController(IAuthService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            this.service = service;
        }

        [AllowAnonymous]
        [HttpPost("Signup")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(Wrapper<LoginResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Wrapper<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Wrapper<object>), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Wrapper<LoginResponse>>> Signup([FromBody] SignupRequest request)
        {
            var data = await service.SignupAsync(request);
            return StatusCode(StatusCodes.Status201Created, new Wrapper<LoginResponse> { Data = data });
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(Wrapper<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Wrapper<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Wrapper<LoginResponse>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<Wrapper<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var data = await service.LoginAsync(request);
                return Ok(new Wrapper<LoginResponse> { Data = data });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new Wrapper<LoginResponse>
                {
                    Error = new APIError
                    {
                        ErrorCode = StatusCodes.Status401Unauthorized,
                        ErrorMessage = "Correo o contraseña incorrectos."
                    }
                });
            }
        }
    }
}
