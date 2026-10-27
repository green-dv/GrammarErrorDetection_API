using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.PointTransactions;

public sealed class PointTransactionList
{
    [NotEmptyGuid]
    public Guid? Id { get; set; }
    [NotEmptyGuid]
    public Guid? RegistrationId { get; set; }
    [NotEmptyGuid]
    public Guid? UserId { get; set; }
    public int? Amount { get; set; }
    [StringLength(255)]
    public string? Reason { get; set; }
    public DateOnly? GrantedAt { get; set; }
    public bool? Active { get; set; }
    public DateOnly? CreatedAt { get; set; }
}

public sealed class PointTransactionListQuery : PaginationDto<PointTransactionList>
{
    public PointTransactionListQuery()
    {
        PageNumber = 1;
        PageSize = 10;
    }
}
