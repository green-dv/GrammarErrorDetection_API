using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Evaluations;

public class EvaluationUpdate
{
    [NotEmptyGuid]
    public Guid Id { get; set; }
    [NotEmptyGuid]
    public Guid? RegistrationId { get; set; }
    public int? Period { get; set; }
    public int? WritingScore { get; set; }
    public int? GrammarScore { get; set; }
    public int? VocabularyScore { get; set; }
    public int? ReadingScore { get; set; }
    public int? ListeningScore { get; set; }
    public int? OralScore { get; set; }
    public int? AcademicPerformance { get; set; }
    public bool? Active { get; set; }
    /// <summary>NULL conserva el comentario actual; un texto vacío lo borra.</summary>
    [StringLength(500, ErrorMessage = "El comentario admite hasta 500 caracteres.")]
    public string? Comment { get; set; }
}
