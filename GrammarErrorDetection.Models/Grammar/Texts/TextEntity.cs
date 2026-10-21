using System;
using System.Collections.Generic;
using System.Text;

namespace GrammarErrorDetection.Models.Grammar.Texts
{
    public class TextEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;
        public string OriginalContent { get; set; } = string.Empty;
        public string Status { get; set; } = "Pendiente";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool Active { get; set; } = true;

    }
}
