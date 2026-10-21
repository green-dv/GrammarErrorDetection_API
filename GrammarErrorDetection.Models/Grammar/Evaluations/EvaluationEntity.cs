using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Grammar.Evaluations;

public class EvaluationEntity
{
    public Guid Id { get; set; }
    public Guid RegistrationId { get; set; }
    // Solo Evaluation_List devuelve este dato; Insert y Update devuelven el compuesto de tabla.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? UserId { get; set; }
    public int? Period { get; set; }
    public int? WritingScore { get; set; }
    public int? GrammarScore { get; set; }
    public int? VocabularyScore { get; set; }
    public int? ReadingScore { get; set; }
    public int? ListeningScore { get; set; }
    public int? OralScore { get; set; }
    public int? AcademicPerformance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Active { get; set; } = true;
}
