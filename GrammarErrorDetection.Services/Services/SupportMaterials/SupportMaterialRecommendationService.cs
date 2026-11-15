using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.SupportMaterials;

/// <summary>
/// Recomienda material de apoyo según las clasificaciones de error más frecuentes del estudiante.
/// El ranking lo calcula SupportMaterial_Recommendation_List; aquí se aplica el control de acceso
/// y se agrupan las filas por clasificación.
/// </summary>
public sealed class SupportMaterialRecommendationService(ISupportMaterialRepository materials, IClassRepository classes)
    : ISupportMaterialRecommendationService
{
    public async Task<List<SupportMaterialRecommendationDto>> Recommend(ClaimsPrincipal user,
        SupportMaterialRecommendationQuery request)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new ValidationContext(request), true);
        var (userId, classId) = await ResolveScope(user, request);

        var rows = await materials.ListRecommendations(userId, classId, request.CategoryLimit, request.MaterialsPerCategory);
        return rows
            .GroupBy(row => row.Material.ErrorClassificationId)
            .Select(group =>
            {
                var first = group.First();
                return new SupportMaterialRecommendationDto
                {
                    ErrorClassificationId = group.Key,
                    ErrorClassificationName = first.ErrorClassificationName,
                    ErrorClassificationDescription = first.ErrorClassificationDescription,
                    ErrorCount = first.ErrorCount,
                    RelatedTextCount = first.RelatedTextCount,
                    ErrorPercentage = first.ErrorPercentage,
                    LastErrorAt = first.LastErrorAt,
                    Rank = first.CategoryRank,
                    Materials = group.Select(row => row.Material).OrderBy(material => material.Rank).ToList()
                };
            })
            .OrderBy(category => category.Rank)
            .ToList();
    }

    // Estudiante: solo sus propios errores. Docente: estudiantes de sus cursos (ClassId obligatorio).
    // Coordinador: cualquier estudiante. La inscripción aprobada en ClassId la valida la base de datos.
    private async Task<(Guid UserId, Guid? ClassId)> ResolveScope(ClaimsPrincipal user, SupportMaterialRecommendationQuery request)
    {
        if (user.Identity?.IsAuthenticated != true
            || !Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var actor) || actor == Guid.Empty)
            throw new UnauthorizedAccessException();

        if (user.IsInRole("Coordinator"))
            return (request.UserId ?? throw new ValidationException("UserId es obligatorio."), request.ClassId);

        if (user.IsInRole("Teacher"))
        {
            if (request.UserId is not Guid student) throw new ValidationException("UserId es obligatorio.");
            if (request.ClassId is not Guid classId) throw new ValidationException("ClassId es obligatorio para docentes.");
            var course = (await classes.List(new PaginationDto<ClassList>
            {
                PageNumber = 1, PageSize = 1, Filter = new() { Id = classId, TeacherId = actor }
            })).SingleOrDefault(item => item.Id == classId);
            if (course is null || course.TeacherId != actor) Denied();
            return (student, classId);
        }

        if (user.IsInRole("Student"))
        {
            if (request.UserId.HasValue && request.UserId != actor) Denied();
            return (actor, request.ClassId);
        }

        Denied();
        return default;
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Denied() =>
        throw new ReportAccessDeniedException("No tienes acceso a las recomendaciones del estudiante solicitado.");
}
