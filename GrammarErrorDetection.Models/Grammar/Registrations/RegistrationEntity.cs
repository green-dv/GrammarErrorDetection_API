namespace GrammarErrorDetection.Models.Grammar.Registrations
{
    public class RegistrationEntity
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid? ClassId { get; set; }

        public DateTime? RegistrationDate { get; set; }

        public string Status { get; set; } = "Pending";

        public string? InvoiceLink { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool Active { get; set; } = true;
    }
}

