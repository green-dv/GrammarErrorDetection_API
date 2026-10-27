using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.PointTransactions;

public sealed class PointTransactionInsert
{
    [NotEmptyGuid]
    public Guid RegistrationId { get; set; }
    [Required]
    public int? Amount { get; set; }
    [Required, StringLength(255)]
    public string Reason { get; set; } = string.Empty;
    public DateTime? GrantedAt { get; set; }
    public bool Active { get; set; } = true;
}
