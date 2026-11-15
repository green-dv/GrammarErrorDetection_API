using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class ChallengeUpdate : IValidatableObject
{
    [NotEmptyGuid]
    public Guid Id { get; set; }
    [StringLength(150)]
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    [StringLength(20)]
    public string? Difficulty { get; set; }
    [Range(0, int.MaxValue)]
    public int? Points { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [StringLength(20)] [RegularExpression(@"^(Draft|Published|Closed)$")]
    public string? Status { get; set; }
    public bool? Active { get; set; }
    /// <summary>null conserva las condiciones; una lista las reemplaza (solo en borrador).</summary>
    public List<GamificationConditionInput>? Conditions { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (StartDate.HasValue && StartDate.Value.Kind != DateTimeKind.Unspecified || EndDate.HasValue && EndDate.Value.Kind != DateTimeKind.Unspecified)
            yield return new ValidationResult("Las fechas deben ser hora local de Bolivia sin sufijo Z ni zona horaria.");
        if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
            yield return new ValidationResult("EndDate no puede ser anterior a StartDate.");
        foreach (var result in GamificationConditionInput.ValidateList(Conditions, nameof(Conditions)))
            yield return result;
    }
}
