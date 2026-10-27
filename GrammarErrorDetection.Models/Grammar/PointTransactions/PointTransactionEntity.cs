namespace GrammarErrorDetection.Models.Grammar.PointTransactions;

public sealed class PointTransactionEntity
{
    public Guid Id { get; set; }
    public Guid RegistrationId { get; set; }
    public int Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Active { get; set; }
}
