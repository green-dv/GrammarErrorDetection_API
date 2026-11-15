using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Validation;

/// <summary>
/// Contraseña segura: al menos 8 caracteres, una mayúscula, una minúscula, un número y un carácter especial
/// (cualquier carácter que no sea letra, número ni espacio).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class StrongPasswordAttribute : ValidationAttribute
{
    public const int MinimumLength = 8;
    public const string Requirements = "La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula, un número y un carácter especial.";

    public StrongPasswordAttribute() : base(Requirements) { }

    public static bool IsStrong(string? value) => value is { Length: >= MinimumLength }
        && value.Any(char.IsUpper) && value.Any(char.IsLower) && value.Any(char.IsDigit)
        && value.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));

    // Null lo resuelve [Required]; aquí solo se valida el contenido.
    public override bool IsValid(object? value) => value is null || value is string text && IsStrong(text);
}
