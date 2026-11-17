namespace GrammarErrorDetection.Models.Auth
{
    public class UserAuthResponse
    {
        public Guid Id { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
