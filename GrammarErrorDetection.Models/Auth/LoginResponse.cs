namespace GrammarErrorDetection.Models.Auth
{
    public class LoginResponse
    {
        /// <summary>Vacío mientras <see cref="RequiresPasswordChange"/> sea true: el JWT se entrega tras cambiar la contraseña.</summary>
        public string AccessToken { get; set; } = string.Empty;

        public string TokenType { get; set; } = "Bearer";

        public DateTimeOffset ExpiresAt { get; set; }
        public UserAuthResponse User { get; set; } = new();

        /// <summary>La cuenta tiene una contraseña temporal o un cambio exigido por el coordinador.</summary>
        public bool RequiresPasswordChange { get; set; }

        /// <summary>Ticket de un solo uso para POST api/Auth/Login/ChangePassword.</summary>
        public PasswordChangeTicket? PasswordChange { get; set; }
    }

    public sealed class PasswordChangeTicket
    {
        public Guid ChallengeId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
