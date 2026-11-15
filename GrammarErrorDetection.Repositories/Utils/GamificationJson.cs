using System.Data;
using System.Text.Json;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Repositories.Utils;

/// <summary>
/// Condiciones de logros y desafíos como JSONB: lista que reciben los SP de alta/edición
/// (gamification_set_conditions) y la que devuelven los listados.
/// </summary>
public static class GamificationJson
{
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web);

    /// <summary>null conserva las condiciones actuales (solo en ediciones).</summary>
    public static string? Conditions(IReadOnlyList<GamificationConditionInput>? conditions) =>
        conditions is null ? null : JsonSerializer.Serialize(conditions.Select(condition => new
        {
            condition.ConditionTypeId,
            condition.ComparisonOperator,
            condition.TargetValue,
            ConditionParameters = condition.ConditionParameters is { ValueKind: JsonValueKind.Object } parameters ? parameters : (JsonElement?)null
        }));

    public static List<T> ReadList<T>(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column))
            throw new DataException($"Falta la columna {column}.");
        if (row.IsNull(column)) return [];
        var raw = row[column] as string ?? row[column].ToString() ?? "[]";
        return JsonSerializer.Deserialize<List<T>>(raw, ReadOptions) ?? [];
    }
}
