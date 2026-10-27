using GrammarErrorDetection.Models.Grammar.PointTransactions;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IPointTransactionRepository
{
    Task<List<PointTransactionEntity>> List(PointTransactionListQuery request);
    Task<PointTransactionEntity> Insert(PointTransactionInsert request);
    Task<PointTransactionEntity> Update(PointTransactionUpdate request);
    Task<PointTransactionTotalDto> Total(PointTransactionTotalQuery request);
}
