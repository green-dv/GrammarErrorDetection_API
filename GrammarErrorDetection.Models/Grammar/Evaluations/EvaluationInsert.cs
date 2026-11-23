using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Evaluations;

public class EvaluationInsert
{
    [NotEmptyGuid]
    public Guid RegistrationId { get; set; }
    public int? Period { get; set; }
    public int? WritingScore { get; set; }
    public int? GrammarScore { get; set; }
    public int? VocabularyScore { get; set; }
    public int? ReadingScore { get; set; }
    public int? ListeningScore { get; set; }
    public int? OralScore { get; set; }
    public int? AcademicPerformance { get; set; }
    public bool Active { get; set; } = true;
    [StringLength(500, ErrorMessage = "El comentario admite hasta 500 caracteres.")]
    public string? Comment { get; set; }
}
