namespace GrammarErrorDetection.Models.Grammar.Users
{
    public class UserUpdate
    {
        public Guid Id { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? CI { get; set; }
        public string? Email { get; set; }

        // Valor preparado para persistencia; no representa la contraseña en texto plano del registro.
        public string? Password { get; set; }
        public string? Phone { get; set; }
        public Guid? RoleId { get; set; }

        public bool? HasAuthenticator { get; set; }
        public string? TotpSecret { get; set; }

        public bool? IsTotpEnabled { get; set; }

        public bool? ChangePassword { get; set; }

        public bool? Active { get; set; }
    }
}
