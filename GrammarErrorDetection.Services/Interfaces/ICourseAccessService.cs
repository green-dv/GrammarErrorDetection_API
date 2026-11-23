namespace GrammarErrorDetection.Services.Interfaces;

/// <summary>
/// Límite de los docentes en la administración de cursos: solo gestionan los cursos donde son el docente asignado
/// (los que ellos crean quedan asignados a ellos). Los coordinadores no tienen este límite.
/// </summary>
public interface ICourseAccessService
{
    /// <summary>Falla con AccessDeniedException si el curso no existe a su nombre.</summary>
    Task EnsureOwnsClass(Guid teacherId, Guid classId);

    /// <summary>Falla si la inscripción no pertenece a un curso del docente (o no tiene curso).</summary>
    Task EnsureOwnsRegistration(Guid teacherId, Guid registrationId);

    /// <summary>Ids de todos los cursos del docente.</summary>
    Task<HashSet<Guid>> OwnedClassIds(Guid teacherId);
}
