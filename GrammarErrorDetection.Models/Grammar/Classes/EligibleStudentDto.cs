namespace GrammarErrorDetection.Models.Grammar.Classes;

public sealed class EligibleStudentDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CI { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string EligibilityReason { get; set; } = string.Empty;
    public decimal? LastFinalScore { get; set; }
    public bool IsAssigned { get; set; }
    public string RegistrationStatus { get; set; } = "Pending";
    public List<StudentAssignmentDto> Assignments { get; set; } = [];
}

public sealed class StudentAssignmentDto
{
    public Guid RegistrationId { get; set; }
    public Guid ClassId { get; set; }
    public Guid ModuleNameId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string? Parallel { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string RegistrationStatus { get; set; } = string.Empty;
}
