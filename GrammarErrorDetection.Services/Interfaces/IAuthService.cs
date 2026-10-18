using GrammarErrorDetection.Models.Auth;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
    }
}
