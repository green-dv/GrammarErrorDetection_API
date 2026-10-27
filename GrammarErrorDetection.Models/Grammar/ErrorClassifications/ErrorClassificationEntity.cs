namespace GrammarErrorDetection.Models.Grammar.ErrorClassifications;

public sealed class ErrorClassificationEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
