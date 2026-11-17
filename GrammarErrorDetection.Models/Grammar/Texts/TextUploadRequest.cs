using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Texts;

public sealed class TextUploadRequest
{
    [GrammarErrorDetection.Models.Grammar.Validation.NotEmptyGuid] public Guid? RegistrationId { get; set; }

    [Required]
    public string OriginalContent { get; set; } = string.Empty;
}
