namespace GrammarErrorDetection.Models.Auth
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public string TokenType { get; set; } = "Bearer";

        public DateTimeOffset ExpiresAt { get; set; }
        public UserAuthResponse User { get; set; } = new();
    }
}
