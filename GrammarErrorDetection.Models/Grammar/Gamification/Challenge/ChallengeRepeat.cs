using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

/// <summary>Nueva ronda de un desafío publicado o cerrado: vuelve a habilitarlo para todos con nuevas fechas.</summary>
public sealed class ChallengeRepeat : IValidatableObject
{
    [Required]
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (StartDate.HasValue && StartDate.Value.Kind != DateTimeKind.Unspecified || EndDate.HasValue && EndDate.Value.Kind != DateTimeKind.Unspecified)
            yield return new ValidationResult("Las fechas deben ser hora local de Bolivia sin sufijo Z ni zona horaria.");
        if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
            yield return new ValidationResult("EndDate no puede ser anterior a StartDate.");
    }
}
