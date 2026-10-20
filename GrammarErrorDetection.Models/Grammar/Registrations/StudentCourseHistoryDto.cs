using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Registrations;

public sealed class StudentCourseHistoryQuery
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, int.MaxValue)]
    public int PageSize { get; set; } = 20;
}

public sealed class StudentCourseHistoryDto
{
    public Guid RegistrationId { get; set; }
    public Guid ClassId { get; set; }
    public Guid ModuleId { get; set; }
    public string? ModuleName { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string? TeacherName { get; set; }
    public decimal? Exam1Score { get; set; }
    public decimal? Exam2Score { get; set; }
    public string? Parallel { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int Year { get; set; }
    public int Bimester { get; set; }
    public string Status { get; set; } = string.Empty;
}
