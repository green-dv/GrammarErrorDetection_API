namespace GrammarErrorDetection.Services.Configuration;

public sealed class VerificationCodeOptions
{
    public const string EnvironmentVariable = "VERIFICATION_CODE_EXPIRATION_MINUTES";
    public int ExpirationMinutes { get; }

    public VerificationCodeOptions(int expirationMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(expirationMinutes, 1);
        ExpirationMinutes = expirationMinutes;
    }
}
