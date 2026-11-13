using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class ChallengeInsert : IValidatableObject
{
    [Required] [StringLength(150)]
    public string Title { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    [Required] [StringLength(20)]
    public string Difficulty { get; set; } = "Normal";
    [Range(0, int.MaxValue)]
    public int Points { get; set; } = 0;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [Required] [StringLength(20)] [RegularExpression(@"^(Draft|Published|Closed)$")]
    public string Status { get; set; } = "Draft";
    public bool Active { get; set; } = true;
    /// <summary>Se completa cuando se cumplen todas. Publicar requiere al menos una.</summary>
    public List<GamificationConditionInput> Conditions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (StartDate.HasValue && StartDate.Value.Kind != DateTimeKind.Unspecified || EndDate.HasValue && EndDate.Value.Kind != DateTimeKind.Unspecified)
            yield return new ValidationResult("Las fechas deben ser hora local de Bolivia sin sufijo Z ni zona horaria.");
        if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
            yield return new ValidationResult("EndDate no puede ser anterior a StartDate.");
        if (Status == "Published" && (!StartDate.HasValue || Conditions is not { Count: > 0 }))
            yield return new ValidationResult("Publicar requiere StartDate y al menos una condición.");
        foreach (var result in GamificationConditionInput.ValidateList(Conditions, nameof(Conditions)))
            yield return result;
    }
}
