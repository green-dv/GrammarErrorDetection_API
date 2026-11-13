namespace GrammarErrorDetection.Models.Grammar.SupportMaterials;

// Fila de grammar."SupportMaterialInteraction": lo que un estudiante hizo con un material.
public sealed class SupportMaterialInteractionEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SupportMaterialId { get; set; }
    public Guid? RegistrationId { get; set; }
    public DateTime FirstViewedAt { get; set; }
    public DateTime LastViewedAt { get; set; }
    public int ViewCount { get; set; }
    public bool Downloaded { get; set; }
    // null = el estudiante todavía no respondió "¿Te sirvió?".
    public bool? Helpful { get; set; }
    public DateTime? FeedbackAt { get; set; }
    public string? Comment { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
