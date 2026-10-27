using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.PointTransactions;

public sealed class PointTransactionUpdate
{
    [NotEmptyGuid]
    public Guid Id { get; set; }
    [NotEmptyGuid]
    public Guid? RegistrationId { get; set; }
    public int? Amount { get; set; }
    [StringLength(255)]
    public string? Reason { get; set; }
    public DateTime? GrantedAt { get; set; }
    public bool? Active { get; set; }
}
