using GrammarErrorDetection.Models.Grammar.PointTransactions;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IPointTransactionService
{
    Task<List<PointTransactionEntity>> List(PointTransactionListQuery request);
    Task<PointTransactionEntity> Insert(PointTransactionInsert request);
    Task<PointTransactionEntity> Update(PointTransactionUpdate request);
    Task<PointTransactionTotalDto> Total(PointTransactionTotalQuery request);
}
