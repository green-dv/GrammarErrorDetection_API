using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Registrations;

public sealed class RegistrationSummaryList
{
    [NotEmptyGuid]
    public Guid? UserId { get; set; }

    [NotEmptyGuid]
    public Guid? ModuleNameId { get; set; }

    public DateOnly? RegistrationDate { get; set; }

    [StringLength(30)]
    public string? Status { get; set; }
}
