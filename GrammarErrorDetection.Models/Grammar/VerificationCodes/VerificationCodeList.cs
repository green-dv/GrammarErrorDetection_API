using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.VerificationCodes
{
    public class VerificationCodeList
    {
        [NotEmptyGuid]
        public Guid? Id { get; set; }

        [StringLength(255)]
        public string? CodeHash { get; set; }

        [StringLength(50)]
        public string? Purpose { get; set; }

        [Range(0, int.MaxValue)]
        public int? AttemptCount { get; set; }

        public DateOnly? ConsumedAt { get; set; }

        public DateOnly? ExpirationTime { get; set; }

        [NotEmptyGuid]
        public Guid? UserId { get; set; }

        [StringLength(255)]
        public string? Email { get; set; }

        public bool? Active { get; set; }

        public DateOnly? CreatedAt { get; set; }
    }
}
