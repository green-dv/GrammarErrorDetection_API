using GrammarErrorDetection.Models.Auth;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthChallengeResponse> LoginAsync(LoginRequest request);
        Task<AuthChallengeResponse> SendSignupVerificationCodeAsync(SignupRequest request);
        Task<LoginResponse> SignupAsync(SignupConfirmationRequest request);
        Task<LoginResponse> VerifyMfaLoginAsync(VerifyMfaRequest request);
        Task<TotpSetupResponse> GenerateTotpSetupAsync(Guid userId, string email);
        Task ConfirmTotpSetupAsync(Guid userId, string email, TotpCodeRequest request);
    }
}
