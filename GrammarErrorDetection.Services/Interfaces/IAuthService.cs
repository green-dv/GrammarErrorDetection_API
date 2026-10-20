using GrammarErrorDetection.Models.Auth;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginOptionsResponse> LoginAsync(LoginRequest request);
        Task<AuthChallengeResponse> SendVerificationCodeAsync(SendVerificationCodeRequest request);
        Task<LoginResponse> SignupAsync(SignupConfirmationRequest request);
        Task<LoginResponse> VerifyMfaLoginAsync(VerifyMfaRequest request);
        Task<LoginResponse> VerifyTotpLoginAsync(VerifyTotpLoginRequest request);
        Task<TotpSetupResponse> GenerateTotpSetupAsync(Guid userId, string email);
        Task ConfirmTotpSetupAsync(Guid userId, string email, TotpCodeRequest request);
    }
}
