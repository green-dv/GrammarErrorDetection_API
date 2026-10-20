namespace GrammarErrorDetection.Models.Grammar.Users;

public sealed class CurrentCourseDto : CourseSummaryDto
{
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public Guid ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public int ClassroomCapacity { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public Guid RegistrationId { get; set; }
    public DateTime? RegistrationDate { get; set; }
}
