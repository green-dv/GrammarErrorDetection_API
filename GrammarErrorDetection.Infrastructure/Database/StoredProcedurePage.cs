using System.Data;

namespace GrammarErrorDetection.Infrastructure.Database
{
    /// <summary>
    /// Resultado de un SP de listado con el formato de dos DataTables:
    /// <see cref="Rows"/> es la página que cumple los filtros y <see cref="TotalCount"/> el total de registros
    /// que cumplen esos filtros sin aplicar la paginación (para calcular el número de páginas).
    /// </summary>
    public sealed class StoredProcedurePage : IDisposable
    {
        public StoredProcedurePage(DataTable rows, DataTable total)
        {
            ArgumentNullException.ThrowIfNull(rows);
            ArgumentNullException.ThrowIfNull(total);
            Rows = rows;
            Total = total;
        }

        public DataTable Rows { get; }
        public DataTable Total { get; }

        /// <summary>Primera columna de la primera fila del segundo DataTable.</summary>
        public long TotalCount
        {
            get
            {
                if (Total.Rows.Count != 1 || Total.Columns.Count == 0 || Total.Rows[0][0] is DBNull)
                    throw new DataException("El total del listado debe ser exactamente una fila con un valor.");
                return Convert.ToInt64(Total.Rows[0][0], System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public void Dispose()
        {
            Rows.Dispose();
            Total.Dispose();
        }
    }
}
