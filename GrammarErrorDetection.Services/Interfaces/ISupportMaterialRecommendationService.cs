using System.Security.Claims;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;

namespace GrammarErrorDetection.Services.Interfaces;

public interface ISupportMaterialRecommendationService
{
    Task<List<SupportMaterialRecommendationDto>> Recommend(ClaimsPrincipal user, SupportMaterialRecommendationQuery request);
}
