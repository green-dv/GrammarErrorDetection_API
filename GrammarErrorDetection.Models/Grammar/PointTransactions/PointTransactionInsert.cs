using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.PointTransactions;

public sealed class PointTransactionInsert : IValidatableObject
{
    [NotEmptyGuid]
    public Guid? RegistrationId { get; set; }
    [NotEmptyGuid]
    public Guid? UserId { get; set; }
    [Required]
    public int? Amount { get; set; }
    [Required, StringLength(255)]
    public string Reason { get; set; } = string.Empty;
    public DateTime? GrantedAt { get; set; }
    public bool Active { get; set; } = true;
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RegistrationId is null && UserId is null)
            yield return new ValidationResult("UserId es obligatorio cuando no hay inscripción.", [nameof(UserId)]);
    }
}
