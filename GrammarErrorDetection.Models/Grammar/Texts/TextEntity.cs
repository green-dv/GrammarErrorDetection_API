using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarErrorDetection.Models.Grammar.Texts
{
    public class TextEntity
    {
        Guid Id { get; set; }
        Guid UserId { get; set; }
        DateTime SubmissionDate { get; set; } = DateTime.UtcNow;
        string OriginalContent { get; set; } = string.Empty;
        string Status { get; set; } = "Pendiente";
        DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
