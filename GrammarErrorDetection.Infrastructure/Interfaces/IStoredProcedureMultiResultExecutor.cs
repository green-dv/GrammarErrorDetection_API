using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Responses;

namespace GrammarErrorDetection.Infrastructure.Interfaces
{
    /// <summary>
    /// Ejecuta un SP que devuelve varios DataTables mediante cursores INOUT con nombre.
    /// Pensado para resúmenes sin paginar (KPIs, top N); los listados paginados usan <see cref="IStoredProcedurePageExecutor"/>.
    /// </summary>
    public interface IStoredProcedureMultiResultExecutor
    {
        Task<ResourceResponse<StoredProcedureResultSet>> ExecuteMultiResultStoredProcedureAsync(
            string schema,
            string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters,
            IReadOnlyList<string> cursorParameters);
    }
}
