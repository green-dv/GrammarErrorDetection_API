using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Texts;

public sealed class TextUploadRequest
{
    [Required]
    public string OriginalContent { get; set; } = string.Empty;
}
