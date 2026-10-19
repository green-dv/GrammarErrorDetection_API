using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Registrations
{
    public class RegistrationInsert
    {
        [NotEmptyGuid]
        public Guid UserId { get; set; }

        [NotEmptyGuid]
        public Guid? ClassId { get; set; }

        public DateTime? RegistrationDate { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [StringLength(255)]
        public string? InvoiceLink { get; set; }

        public bool Active { get; set; } = true;
    }
}

