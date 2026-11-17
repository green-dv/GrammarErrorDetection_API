using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.Grammar.PointTransactions;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IPointTransactionService
{
    Task<List<PointTransactionEntity>> List(PointTransactionListQuery request);
    /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina (las implementaciones reales lo sobrescriben).</summary>
    async Task<PagedList<PointTransactionEntity>> ListPage(PointTransactionListQuery request)
    {
        var items = await List(request);
        return new PagedList<PointTransactionEntity>(items, items.Count);
    }
    Task<PointTransactionEntity> Insert(PointTransactionInsert request);
    Task<PointTransactionEntity> Update(PointTransactionUpdate request);
    Task<PointTransactionTotalDto> Total(PointTransactionTotalQuery request);
}
