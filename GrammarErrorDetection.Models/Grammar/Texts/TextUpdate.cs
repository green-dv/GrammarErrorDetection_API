using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GrammarErrorDetection.Models.Grammar.Texts
{
    public class TextUpdate
    {
        [Required]
        Guid Id { get; set; }
        [Required]
        Guid UserId { get; set; }
        [Required]
        DateTime SubmissionDate { get; set; } = DateTime.UtcNow;
        [Required]
        string OriginalContent { get; set; } = string.Empty;
        [Required]
        string Status { get; set; } = "Pendiente";
    }
}
