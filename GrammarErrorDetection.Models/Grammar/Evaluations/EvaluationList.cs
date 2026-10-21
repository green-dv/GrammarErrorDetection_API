using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Evaluations;

public class EvaluationList
{
    [NotEmptyGuid]
    public Guid? Id { get; set; }
    [NotEmptyGuid]
    public Guid? RegistrationId { get; set; }
    [NotEmptyGuid]
    public Guid? UserId { get; set; }
    public int? Period { get; set; }
    public int? WritingScore { get; set; }
    public int? GrammarScore { get; set; }
    public int? VocabularyScore { get; set; }
    public int? ReadingScore { get; set; }
    public int? ListeningScore { get; set; }
    public int? OralScore { get; set; }
    public int? AcademicPerformance { get; set; }
    public DateOnly? CreatedAt { get; set; }
    public bool? Active { get; set; }
}
