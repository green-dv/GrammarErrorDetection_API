using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Registrations;

public sealed class StudentRegistrationRequest
{
    [NotEmptyGuid]
    public Guid ClassId { get; set; }

    [StringLength(255)]
    public string? InvoiceLink { get; set; }
}

public sealed class RegistrationDecisionRequest
{
    [Required]
    public bool? Approved { get; set; }
}

public sealed class AvailableRegistrationCourseList
{
    [Range(1, 9999)]
    public int Year { get; set; }

    [Range(1, 6)]
    public int Bimester { get; set; }
}

public sealed class AvailableRegistrationCourseDto
{
    public Guid ClassId { get; set; }
    public Guid ModuleNameId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public Guid ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string? Parallel { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Year { get; set; }
    public int Bimester { get; set; }
    public int AvailableSeats { get; set; }
}
