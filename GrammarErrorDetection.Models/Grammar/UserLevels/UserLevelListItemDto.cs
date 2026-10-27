namespace GrammarErrorDetection.Models.Grammar.UserLevels;

public sealed class UserLevelListItemDto
{
    public Guid Id { get; set; }
    public int Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public long MinPoints { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Active { get; set; }
    public long TotalCount { get; set; }
}
