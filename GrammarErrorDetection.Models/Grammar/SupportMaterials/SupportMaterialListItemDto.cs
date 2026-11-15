namespace GrammarErrorDetection.Models.Grammar.SupportMaterials;

public sealed class SupportMaterialListItemDto : SupportMaterialEntity
{
    public string ErrorClassificationName { get; set; } = string.Empty;
    public string UploadedByName { get; set; } = string.Empty;
    // Interacción del estudiante que consulta (null si quien consulta no es estudiante).
    public bool? ViewerSeen { get; set; }
    public bool? ViewerHelpful { get; set; }
}
