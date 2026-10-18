namespace GrammarErrorDetection.Models.Grammar.VerificationCodes
{
    public class VerificationCodeEntity
    {
        public Guid Id { get; set; }

        public string CodeHash { get; set; } = string.Empty;

        public string Purpose { get; set; } = string.Empty;

        public int AttemptCount { get; set; }

        public DateTime? ConsumedAt { get; set; }

        public DateTime ExpirationTime { get; set; }

        public Guid? UserId { get; set; }

        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool Active { get; set; } = true;
    }
}
