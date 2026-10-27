namespace GrammarErrorDetection.Models.Grammar.Users;

public sealed class UserLeaderboardItemDto
{
    public long? Position { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public Guid? ModuleNameId { get; set; }
    public string? ModuleName { get; set; }
    public long TotalPoints { get; set; }
    public bool IsCurrentUser { get; set; }
    public bool IsTop10 { get; set; }
}
