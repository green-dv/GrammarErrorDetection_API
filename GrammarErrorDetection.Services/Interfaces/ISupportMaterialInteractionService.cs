using System.Security.Claims;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;

namespace GrammarErrorDetection.Services.Interfaces;

public interface ISupportMaterialInteractionService
{
    Task<SupportMaterialInteractionEntity> RegisterView(ClaimsPrincipal user, Guid supportMaterialId, SupportMaterialViewRequest request);
    Task<SupportMaterialInteractionEntity> SetFeedback(ClaimsPrincipal user, Guid supportMaterialId, SupportMaterialFeedbackRequest request);
}
