using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

/// <summary>
/// Condición de un logro o desafío (tabla GamificationCondition). Un logro o desafío se cumple
/// cuando se cumplen todas sus condiciones.
/// </summary>
public sealed class GamificationConditionInput : IValidatableObject
{
    [NotEmptyGuid]
    public Guid ConditionTypeId { get; set; }

    [Required] [StringLength(30)] [RegularExpression(@"^(>=|>|=)$")]
    public string ComparisonOperator { get; set; } = ">=";

    [PositiveIntegerTarget]
    public decimal TargetValue { get; set; }

    /// <summary>Parámetros del tipo de condición, p. ej. {"MinimumWords":20} o {"MinimumScore":80}.</summary>
    public JsonElement? ConditionParameters { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (ConditionTypeId == Guid.Empty)
            yield return new ValidationResult("ConditionTypeId es obligatorio.", [nameof(ConditionTypeId)]);
        if (ConditionParameters is { } parameters && parameters.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null or JsonValueKind.Undefined))
            yield return new ValidationResult("ConditionParameters debe ser un objeto JSON.", [nameof(ConditionParameters)]);
    }

    /// <summary>Valida cada condición de una lista (DataAnnotations no valida colecciones anidadas).</summary>
    public static IEnumerable<ValidationResult> ValidateList(IReadOnlyList<GamificationConditionInput>? conditions, string member)
    {
        if (conditions is null) yield break;
        if (conditions.Count > 10)
            yield return new ValidationResult("Se permiten hasta 10 condiciones.", [member]);
        for (var index = 0; index < conditions.Count; index++)
        {
            if (conditions[index] is not { } condition)
            {
                yield return new ValidationResult($"La condición {index + 1} es obligatoria.", [member]);
                continue;
            }
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(condition, new ValidationContext(condition), results, true);
            foreach (var result in results)
                yield return new ValidationResult($"Condición {index + 1}: {result.ErrorMessage}", [member]);
        }
    }
}

/// <summary>Condición tal como la devuelven los listados (con el código y nombre del tipo).</summary>
public sealed class GamificationConditionDto
{
    public Guid Id { get; set; }
    public Guid ConditionTypeId { get; set; }
    public string? ConditionCode { get; set; }
    public string? ConditionName { get; set; }
    public string ComparisonOperator { get; set; } = ">=";
    public decimal TargetValue { get; set; }
    public JsonElement? ConditionParameters { get; set; }
}

/// <summary>Avance del estudiante en una condición (perfil y medallas).</summary>
public sealed class GamificationConditionProgress
{
    public Guid ConditionId { get; set; }
    public Guid ConditionTypeId { get; set; }
    public string? ConditionCode { get; set; }
    public string? ConditionName { get; set; }
    public string ComparisonOperator { get; set; } = ">=";
    public decimal TargetValue { get; set; }
    public JsonElement? ConditionParameters { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? RequiredValue { get; set; }
    public bool ConditionMet { get; set; }
    public bool CanReach { get; set; }
    public decimal? ProgressPercent { get; set; }
}
