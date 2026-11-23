using GrammarErrorDetection.Models.Grammar.Registrations;

namespace GrammarErrorDetection.Services.Interfaces;

public interface ICourseInvitationService
{
    /// <summary>Genera el token de invitación; solo el docente del curso o un coordinador.</summary>
    Task<CourseInvitationLinkDto> CreateLink(Guid userId, bool isCoordinator, Guid classId);

    /// <summary>Datos del curso de la invitación para quien no puede inscribirse (docente/coordinador); no inscribe ni valida requisitos.</summary>
    Task<CourseJoinResultDto> Describe(string token);

    /// <summary>Valida la invitación para el estudiante y devuelve los datos del curso, sin inscribirlo.</summary>
    Task<CourseJoinResultDto> Preview(Guid studentId, string token);

    /// <summary>Inscribe al estudiante en el curso de la invitación (sin exigir el curso anterior).</summary>
    Task<CourseJoinResultDto> Join(Guid studentId, string token);
}
