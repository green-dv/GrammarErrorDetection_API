namespace GrammarErrorDetection.Models.Grammar.SupportMaterials;

// Una clasificación de error del estudiante con el material recomendado para reforzarla.
public sealed class SupportMaterialRecommendationDto
{
    public Guid ErrorClassificationId { get; set; }
    public string ErrorClassificationName { get; set; } = string.Empty;
    public string? ErrorClassificationDescription { get; set; }
    public long ErrorCount { get; set; }
    public long RelatedTextCount { get; set; }
    public decimal? ErrorPercentage { get; set; }
    public DateTime? LastErrorAt { get; set; }
    // Prioridad de la clasificación (1 = la que más errores acumula).
    public int Rank { get; set; }
    public List<RecommendedMaterialDto> Materials { get; set; } = [];
}

// Fila plana devuelta por SupportMaterial_Recommendation_List.
public sealed record SupportMaterialRecommendationRow(
    RecommendedMaterialDto Material,
    string ErrorClassificationName,
    string? ErrorClassificationDescription,
    long ErrorCount,
    long RelatedTextCount,
    decimal? ErrorPercentage,
    DateTime? LastErrorAt,
    int CategoryRank);
