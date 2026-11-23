using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Registrations;

/// <summary>Enlace de invitación a un curso: el token va firmado y expira; no se guarda en la base de datos.</summary>
public sealed class CourseInvitationLinkDto
{
    public Guid ClassId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public sealed class CourseJoinRequest
{
    [Required]
    [StringLength(200, MinimumLength = 20)]
    public string Token { get; set; } = string.Empty;
}

public sealed class CourseJoinResultDto
{
    public Guid RegistrationId { get; set; }
    public Guid ClassId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string? Parallel { get; set; }
    public int Year { get; set; }
    public int Bimester { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? TeacherName { get; set; }
    public string? ClassroomName { get; set; }
    /// <summary>True si el estudiante ya pertenecía al curso (abrir el enlace otra vez no duplica nada).</summary>
    public bool AlreadyMember { get; set; }
}
