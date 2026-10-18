using GrammarErrorDetection.Models.Grammar.VerificationCodes;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IAuthChallengeRepository
{
    Task<VerificationCodeEntity?> RegisterAttemptAsync(Guid id, string[] purposes, DateTime utcNow, int maxAttempts);
    Task<bool> TryConsumeAsync(Guid id, DateTime utcNow, string? totpStep = null);
    Task<bool> TryEnableTotpAsync(Guid userId, string email, string secret);
    Task<bool> TrySetTotpSecretAsync(Guid userId, string email, string secret);
}
