namespace GrammarErrorDetection.Models.Auth;

public sealed class AuthChallengeResponse
{
    public Guid ChallengeId { get; set; }
    public bool RequiresMfa { get; set; } = true;
    public string Method { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
