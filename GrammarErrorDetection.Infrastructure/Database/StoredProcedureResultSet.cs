using System.Data;

namespace GrammarErrorDetection.Infrastructure.Database
{
    /// <summary>
    /// Resultado de un SP con varios cursores INOUT con nombre (por ejemplo, el resumen de un dashboard).
    /// Cada DataTable se identifica por el nombre del parámetro REFCURSOR que lo devolvió.
    /// </summary>
    public sealed class StoredProcedureResultSet : IDisposable
    {
        private readonly Dictionary<string, DataTable> tables;

        public StoredProcedureResultSet(IReadOnlyDictionary<string, DataTable> tables)
        {
            ArgumentNullException.ThrowIfNull(tables);
            this.tables = new Dictionary<string, DataTable>(tables, StringComparer.Ordinal);
        }

        public IReadOnlyCollection<string> Names => tables.Keys;

        public DataTable this[string cursorParameter] => tables.TryGetValue(cursorParameter, out var table)
            ? table
            : throw new DataException($"El procedimiento no devolvió el cursor {cursorParameter}.");

        public void Dispose()
        {
            foreach (var table in tables.Values) table.Dispose();
        }
    }
}
