using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Auth
{
    public class SignupRequest
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

        [Required]
        [StringLength(72, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [StringLength(12)]
        [Phone]
        public string? Phone { get; set; }
    }
}
