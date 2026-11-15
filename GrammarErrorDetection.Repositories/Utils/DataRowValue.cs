using System.Data;
using System.Globalization;

namespace GrammarErrorDetection.Repositories.Utils;

/// <summary>
/// Lectura tolerante de columnas numéricas: PostgreSQL devuelve sum(bigint) como NUMERIC y count(*) como BIGINT,
/// así que se convierte al tipo del DTO en lugar de exigir el tipo exacto de la columna.
/// </summary>
internal static class DataRowValue
{
    internal static long Long(DataRow row, string column) => LongOrNull(row, column) ?? 0;

    internal static long? LongOrNull(DataRow row, string column)
        => Value(row, column) is { } value ? Convert.ToInt64(value, CultureInfo.InvariantCulture) : null;

    internal static int Int(DataRow row, string column) => IntOrNull(row, column) ?? 0;

    internal static int? IntOrNull(DataRow row, string column)
        => Value(row, column) is { } value ? Convert.ToInt32(value, CultureInfo.InvariantCulture) : null;

    internal static decimal? Decimal(DataRow row, string column)
        => Value(row, column) is { } value ? Convert.ToDecimal(value, CultureInfo.InvariantCulture) : null;

    internal static string? String(DataRow row, string column) => Value(row, column)?.ToString();

    internal static bool Bool(DataRow row, string column) => Value(row, column) is bool value && value;

    internal static Guid? Guid(DataRow row, string column) => Value(row, column) as Guid?;

    internal static DateTime? Date(DataRow row, string column) => Value(row, column) switch
    {
        DateTime value => value,
        DateOnly value => value.ToDateTime(TimeOnly.MinValue),
        _ => null
    };

    internal static TimeSpan? Time(DataRow row, string column) => Value(row, column) switch
    {
        TimeSpan value => value,
        TimeOnly value => value.ToTimeSpan(),
        _ => null
    };

    private static object? Value(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column))
            throw new DataException($"Falta la columna {column}.");
        return row.IsNull(column) ? null : row[column];
    }
}
