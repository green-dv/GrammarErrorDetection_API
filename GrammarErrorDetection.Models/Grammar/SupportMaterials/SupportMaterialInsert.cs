using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.SupportMaterials;

public sealed class SupportMaterialInsert
{
    [Required, StringLength(255)]
    public string Name { get; set; } = string.Empty;
    [NotEmptyGuid]
    public Guid ErrorClassificationId { get; set; }
    [Required]
    public DateTime? UploadDate { get; set; }
    [Required]
    public string FileLink { get; set; } = string.Empty;
    [NotEmptyGuid]
    public Guid FileTypeId { get; set; }
    [NotEmptyGuid]
    public Guid UploadedBy { get; set; }
    [Range(typeof(long), "0", "9223372036854775807")]
    public long? FileSize { get; set; }
    public bool Active { get; set; } = true;
}
