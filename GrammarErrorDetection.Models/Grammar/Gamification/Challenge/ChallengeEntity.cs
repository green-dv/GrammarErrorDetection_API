using System.Text.Json;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

public class ChallengeEntity
{
    public Guid Id { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public int Points { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Ronda vigente: "Repetir" la incrementa y el desafío se puede completar otra vez.
    public int Round { get; set; } = 1;
}
