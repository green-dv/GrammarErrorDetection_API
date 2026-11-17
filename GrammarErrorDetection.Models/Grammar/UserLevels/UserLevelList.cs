using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.UserLevels;

public sealed class UserLevelList : IValidatableObject
{
    [NotEmptyGuid]
    public Guid? Id { get; set; }
    public int? Level { get; set; }
    [StringLength(50)]
    public string? Name { get; set; }
    public long? MinPoints { get; set; }
    public long? MaxPoints { get; set; }
    public bool? Active { get; set; }
    public DateOnly? CreatedAt { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPoints.HasValue && MaxPoints.HasValue && MinPoints > MaxPoints)
            yield return new ValidationResult("MinPoints no puede superar MaxPoints.",
                [nameof(MinPoints), nameof(MaxPoints)]);
    }
}
