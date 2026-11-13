namespace GrammarErrorDetection.Models.Grammar.SupportMaterials;

public sealed class RecommendedMaterialDto : SupportMaterialEntity
{
    public string UploadedByName { get; set; } = string.Empty;
    public string FileTypeName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    // Posición del material dentro de su clasificación (1 = mayor puntaje personalizado).
    public int Rank { get; set; }
    // Si el estudiante ya lo abrió y qué respondió a "¿Te sirvió?" (null = sin respuesta).
    public bool Seen { get; set; }
    public bool? Helpful { get; set; }
    // Puntaje 0..1: 0.35·Efectividad + 0.25·Utilidad + 0.25·Novedad + 0.15·Preferencia de formato.
    public decimal Score { get; set; }
    // Motivo principal: NEW, REVIEW, EFFECTIVE, PEERS_HELPFUL o FORMAT_PREFERENCE.
    public string ReasonCode { get; set; } = string.Empty;
}
