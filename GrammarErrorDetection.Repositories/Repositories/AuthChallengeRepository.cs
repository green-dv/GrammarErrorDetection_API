using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Models.Grammar.VerificationCodes;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class AuthChallengeRepository(
    IVerificationCodeRepository verificationCodes,
    IUserRepository users) : IAuthChallengeRepository
{
    // Coordina este repositorio dentro del mismo proceso. Los SPs actuales
    // no ofrecen comprobación y actualización atómicas entre servidores.
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<VerificationCodeEntity?> RegisterAttemptAsync(Guid id, string[] purposes, DateTime utcNow, int maxAttempts)
    {
        await Gate.WaitAsync();
        try
        {
            var challenge = await FindChallengeAsync(id);
            if (!IsAvailable(challenge, utcNow) || !purposes.Contains(challenge!.Purpose)
                || challenge.AttemptCount >= maxAttempts)
                return null;

            return await verificationCodes.Update(new VerificationCodeUpdate
            {
                Id = id, AttemptCount = challenge.AttemptCount + 1
            });
        }
        finally { Gate.Release(); }
    }

    public async Task<bool> TryConsumeAsync(Guid id, DateTime utcNow, string? totpStep = null)
    {
        await Gate.WaitAsync();
        try
        {
            var challenge = await FindChallengeAsync(id);
            if (!IsAvailable(challenge, utcNow))
                return false;

            if (totpStep is not null)
            {
                if (challenge!.Purpose != "login_totp" || challenge.UserId is null)
                    return false;

                // Los desafíos consumidos están inactivos: no filtrar por Active.
                for (var page = 1; ; page++)
                {
                    var used = await verificationCodes.List(new PaginationDto<VerificationCodeList>
                    {
                        PageNumber = page, PageSize = 100,
                        Filter = new VerificationCodeList
                        {
                            UserId = challenge.UserId, Purpose = "login_totp", CodeHash = totpStep
                        }
                    });
                    if (used.Any(code => code.UserId == challenge.UserId && code.Purpose == "login_totp"
                        && code.CodeHash == totpStep && code.ConsumedAt is not null))
                        return false;
                    if (used.Count < 100)
                        break;
                }
            }

            await verificationCodes.Update(new VerificationCodeUpdate
            {
                Id = id, ConsumedAt = utcNow, Active = false, CodeHash = totpStep
            });
            return true;
        }
        finally { Gate.Release(); }
    }

    public async Task<bool> TryEnableTotpAsync(Guid userId, string email, string secret)
    {
        await Gate.WaitAsync();
        try
        {
            var user = await FindUserAsync(userId, email);
            if (user is null || user.IsTotpEnabled || user.TotpSecret != secret)
                return false;

            await users.Update(new UserUpdate { Id = userId, IsTotpEnabled = true, HasAuthenticator = true });
            return true;
        }
        finally { Gate.Release(); }
    }

    public async Task<bool> TrySetTotpSecretAsync(Guid userId, string email, string secret)
    {
        await Gate.WaitAsync();
        try
        {
            var user = await FindUserAsync(userId, email);
            if (user is null || user.IsTotpEnabled)
                return false;

            await users.Update(new UserUpdate { Id = userId, TotpSecret = secret, HasAuthenticator = false });
            return true;
        }
        finally { Gate.Release(); }
    }

    private async Task<VerificationCodeEntity?> FindChallengeAsync(Guid id)
    {
        var codes = await verificationCodes.List(new PaginationDto<VerificationCodeList>
        {
            PageNumber = 1, PageSize = 1, Filter = new VerificationCodeList { Id = id }
        });
        return codes.SingleOrDefault(code => code.Id == id);
    }

    private async Task<UserEntity?> FindUserAsync(Guid id, string email)
    {
        var matches = await users.List(new PaginationDto<UserList>
        {
            PageNumber = 1, PageSize = 1, Filter = new UserList { Email = email, Active = true }
        });
        return matches.SingleOrDefault(user => user.Id == id && user.Active
            && string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAvailable(VerificationCodeEntity? challenge, DateTime utcNow)
        => challenge is { Active: true, ConsumedAt: null }
            && DatabaseDateTime.ToTimestamp(challenge.ExpirationTime) > DatabaseDateTime.ToTimestamp(utcNow);
}
