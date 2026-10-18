using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Users
{
    public class UserInsert
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string CI { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Valor preparado para persistencia; no representa la contraseña en texto plano del registro.
        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [StringLength(12)]
        [Phone]
        public string? Phone { get; set; }

        [NotEmptyGuid]
        public virtual Guid? RoleId { get; set; }

        public bool HasAuthenticator { get; set; }

        [StringLength(255)]
        public string? TotpSecret { get; set; }

        public bool IsTotpEnabled { get; set; }

        public bool ChangePassword { get; set; }

        public bool Active { get; set; } = true;
    }
}

