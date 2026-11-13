using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class PositiveIntegerTargetAttribute : ValidationAttribute
{
    public PositiveIntegerTargetAttribute() : base("{0} debe ser un entero positivo de hasta 99999999.") { }
    public override bool IsValid(object? value) => value is null
        || value is decimal target && target >= 1 && target <= 99999999 && decimal.Truncate(target) == target;
}
