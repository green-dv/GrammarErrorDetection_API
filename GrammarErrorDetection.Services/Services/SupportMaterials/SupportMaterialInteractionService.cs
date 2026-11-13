using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.SupportMaterials;

/// <summary>
/// Registra lo que el estudiante hace con el material recomendado (vistas y "¿Te sirvió?").
/// Esas interacciones alimentan el puntaje personalizado de SupportMaterial_Recommendation_List.
/// Solo el propio estudiante registra sus interacciones: el usuario sale siempre del token.
/// </summary>
public sealed class SupportMaterialInteractionService(ISupportMaterialRepository materials) : ISupportMaterialInteractionService
{
    public Task<SupportMaterialInteractionEntity> RegisterView(ClaimsPrincipal user, Guid supportMaterialId,
        SupportMaterialViewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var student = ResolveStudent(user);
        ValidateMaterial(supportMaterialId);
        Validator.ValidateObject(request, new ValidationContext(request), true);
        return materials.RegisterView(student, supportMaterialId, request.RegistrationId, request.Downloaded);
    }

    public Task<SupportMaterialInteractionEntity> SetFeedback(ClaimsPrincipal user, Guid supportMaterialId,
        SupportMaterialFeedbackRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var student = ResolveStudent(user);
        ValidateMaterial(supportMaterialId);
        Validator.ValidateObject(request, new ValidationContext(request), true);
        var helpful = request.Helpful ?? throw new ValidationException("Helpful es obligatorio.");
        // El comentario solo explica un "no me sirvió"; con un voto positivo se descarta.
        var comment = helpful || string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        return materials.SetFeedback(student, supportMaterialId, helpful, comment);
    }

    private static Guid ResolveStudent(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (user.Identity?.IsAuthenticated != true
            || !Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var actor) || actor == Guid.Empty)
            throw new UnauthorizedAccessException();
        if (!user.IsInRole("Student"))
            throw new ReportAccessDeniedException("Solo los estudiantes registran su avance con el material de apoyo.");
        return actor;
    }

    private static void ValidateMaterial(Guid supportMaterialId)
    {
        if (supportMaterialId == Guid.Empty) throw new ValidationException("El material es obligatorio.");
    }
}
