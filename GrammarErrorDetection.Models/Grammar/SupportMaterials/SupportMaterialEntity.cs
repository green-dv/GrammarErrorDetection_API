namespace GrammarErrorDetection.Models.Grammar.SupportMaterials;

public class SupportMaterialEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid ErrorClassificationId { get; set; }
    public DateTime UploadDate { get; set; }
    public string FileLink { get; set; } = string.Empty;
    public Guid FileTypeId { get; set; }
    public long? FileSize { get; set; }
    public Guid UploadedBy { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
