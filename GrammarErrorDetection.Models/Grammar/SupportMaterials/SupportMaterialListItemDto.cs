namespace GrammarErrorDetection.Models.Grammar.SupportMaterials;

public sealed class SupportMaterialListItemDto : SupportMaterialEntity
{
    public string ErrorClassificationName { get; set; } = string.Empty;
    public string UploadedByName { get; set; } = string.Empty;
}
