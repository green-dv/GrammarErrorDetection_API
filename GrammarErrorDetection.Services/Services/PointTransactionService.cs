using GrammarErrorDetection.Models.Grammar.PointTransactions;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class PointTransactionService(IPointTransactionRepository repository) : IPointTransactionService
{
    public Task<List<PointTransactionEntity>> List(PointTransactionListQuery request) => repository.List(request);
    public Task<PointTransactionEntity> Insert(PointTransactionInsert request) => repository.Insert(request);
    public Task<PointTransactionEntity> Update(PointTransactionUpdate request) => repository.Update(request);
    public Task<PointTransactionTotalDto> Total(PointTransactionTotalQuery request) => repository.Total(request);
}
