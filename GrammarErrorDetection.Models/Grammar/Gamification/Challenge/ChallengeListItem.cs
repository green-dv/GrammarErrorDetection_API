namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class ChallengeListItem : ChallengeEntity
{
    public List<GamificationConditionDto> Conditions { get; set; } = [];
    // Estudiantes que completaron la ronda vigente.
    public long CompletionCount { get; set; }
}
