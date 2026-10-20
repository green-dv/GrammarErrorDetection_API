using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace GrammarErrorDetection.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("auth")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
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
        public async Task<Wrapper<LoginResponse>> Signup([FromBody] SignupConfirmationRequest request)
        {
            var data = await service.SignupAsync(request);
            Response.StatusCode = StatusCodes.Status201Created;
            return new Wrapper<LoginResponse> { Data = data };
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<Wrapper<LoginOptionsResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var data = await service.LoginAsync(request);
                return new Wrapper<LoginOptionsResponse> { Data = data };
            }
            catch (UnauthorizedAccessException)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return new Wrapper<LoginOptionsResponse>
                {
                    Error = new APIError
                    {
                        ErrorCode = StatusCodes.Status401Unauthorized,
                        ErrorMessage = "Correo o contraseña incorrectos."
                    }
                };
            }
        }

        [AllowAnonymous]
        [HttpPost("SendCode")]
        public async Task<Wrapper<AuthChallengeResponse>> SendCode([FromBody] SendVerificationCodeRequest request)
        {
            var data = await service.SendVerificationCodeAsync(request);
            return new Wrapper<AuthChallengeResponse> { Data = data };
        }

        [AllowAnonymous]
        [HttpPost("Login/Verify")]
        public async Task<Wrapper<LoginResponse>> VerifyLogin([FromBody] VerifyMfaRequest request)
        {
            var data = await service.VerifyMfaLoginAsync(request);
            return new Wrapper<LoginResponse> { Data = data };
        }

        [AllowAnonymous]
        [HttpPost("Login/VerifyTotp")]
        public async Task<Wrapper<LoginResponse>> VerifyTotpLogin([FromBody] VerifyTotpLoginRequest request)
        {
            var data = await service.VerifyTotpLoginAsync(request);
            return new Wrapper<LoginResponse> { Data = data };
        }

        [Authorize]
        [HttpPost("Totp/Setup")]
        public async Task<Wrapper<TotpSetupResponse>> SetupTotp()
        {
            var (id, email) = GetIdentity();
            var data = await service.GenerateTotpSetupAsync(id, email);
            return new Wrapper<TotpSetupResponse> { Data = data };
        }

        [Authorize]
        [HttpPost("Totp/Confirm")]
        public async Task<Wrapper<bool>> ConfirmTotp([FromBody] TotpCodeRequest request)
        {
            var (id, email) = GetIdentity();
            await service.ConfirmTotpSetupAsync(id, email, request);
            return new Wrapper<bool> { Data = true };
        }

        private (Guid Id, string Email) GetIdentity()
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                || string.IsNullOrWhiteSpace(User.FindFirstValue(ClaimTypes.Email)))
                throw new UnauthorizedAccessException();
            return (id, User.FindFirstValue(ClaimTypes.Email)!);
        }
    }
}
