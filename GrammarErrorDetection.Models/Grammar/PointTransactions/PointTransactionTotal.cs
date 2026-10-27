using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.PointTransactions;

public sealed class PointTransactionTotalQuery
{
    [NotEmptyGuid]
    public Guid? ClassId { get; set; }
    [NotEmptyGuid]
    public Guid? UserId { get; set; }
}

public sealed class PointTransactionTotalDto
{
    public long TotalPoints { get; set; }
}
