using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Registrations;

public sealed class RegistrationGenerationRequest : IValidatableObject
{
    // Periodo de DESTINO. El origen siempre se calcula como el inmediatamente anterior.
    [Range(2, 9999)]
    public int Year { get; set; }

    [Range(1, 6)]
    public int Bimester { get; set; }

    [Required]
    public DateTime? StartDate { get; set; }

    [Required]
    public DateTime? EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate is not null && EndDate is not null && EndDate < StartDate)
            yield return new ValidationResult("La fecha final debe ser igual o posterior a la inicial.",
                [nameof(EndDate)]);
        if (StartDate?.Year != Year || EndDate?.Year != Year)
            yield return new ValidationResult("Las fechas deben pertenecer al año de destino.",
                [nameof(StartDate), nameof(EndDate)]);
    }
}
