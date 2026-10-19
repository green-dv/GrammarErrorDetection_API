namespace GrammarErrorDetection.Models.Grammar.Users;

public sealed class UserSummaryDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CI { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsEnrolled { get; set; }
    public CourseSummaryDto? CurrentCourse { get; set; }
    public CourseSummaryDto? LastCourse { get; set; }
    public decimal? LastCourseFinalScore { get; set; }
}
