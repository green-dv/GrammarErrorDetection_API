using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Responses;

namespace GrammarErrorDetection.Infrastructure.Interfaces
{
    /// <summary>
    /// Ejecuta SPs de listado con el formato de dos DataTables (dos cursores INOUT):
    /// la página filtrada y el total de registros que cumplen los filtros.
    /// Interfaz separada de <see cref="IStoredProcedureExecutor"/> para no obligar a cambiar sus implementaciones.
    /// </summary>
    public interface IStoredProcedurePageExecutor
    {
        Task<ResourceResponse<StoredProcedurePage>> ExecutePagedStoredProcedureAsync(
            string schema,
            string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters,
            string rowsCursorParameter = "Results",
            string totalCursorParameter = "Total");
    }
}
