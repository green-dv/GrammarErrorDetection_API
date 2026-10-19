using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Registrations
{
    public class RegistrationUpdate
    {
        [NotEmptyGuid]
        public Guid Id { get; set; }

        [NotEmptyGuid]
        public Guid? UserId { get; set; }

        [NotEmptyGuid]
        public Guid? ClassId { get; set; }

        public DateTime? RegistrationDate { get; set; }

        [StringLength(30)]
        public string? Status { get; set; }

        [StringLength(255)]
        public string? InvoiceLink { get; set; }

        public bool? Active { get; set; }
    }
}

