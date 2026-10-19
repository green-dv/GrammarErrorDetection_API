using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Registrations
{
    public class RegistrationList
    {
        [NotEmptyGuid]
        public Guid? Id { get; set; }

        [NotEmptyGuid]
        public Guid? UserId { get; set; }

        [NotEmptyGuid]
        public Guid? ClassId { get; set; }

        public DateOnly? RegistrationDate { get; set; }

        [StringLength(30)]
        public string? Status { get; set; }

        [StringLength(255)]
        public string? InvoiceLink { get; set; }

        public DateOnly? CreatedAt { get; set; }

        public bool? Active { get; set; }
    }
}

