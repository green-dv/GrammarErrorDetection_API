using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.VerificationCodes
{
    public class VerificationCodeInsert
    {
        [Required]
        [StringLength(255)]
        public string CodeHash { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Purpose { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int AttemptCount { get; set; }

        public DateTime? ConsumedAt { get; set; }

        // La expiracion la calcula el servicio usando la configuracion del servidor.
        [JsonIgnore]
        public DateTime? ExpirationTime { get; set; }

        [NotEmptyGuid]
        public Guid? UserId { get; set; }

        [StringLength(255)]
        [EmailAddress]
        public string? Email { get; set; }

        public bool Active { get; set; } = true;
    }
}
