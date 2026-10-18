namespace GrammarErrorDetection.Models.Grammar.VerificationCodes
{
    public class VerificationCodeUpdate
    {
        public Guid Id { get; set; }
        public string? CodeHash { get; set; }
        public string? Purpose { get; set; }
        public int? AttemptCount { get; set; }

        public DateTime? ConsumedAt { get; set; }
        public DateTime? ExpirationTime { get; set; }

        public Guid? UserId { get; set; }
        public string? Email { get; set; }

        public bool? Active { get; set; }
    }
}
