using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.SupportMaterials;

public sealed class SupportMaterialUpdate
{
    [NotEmptyGuid]
    public Guid Id { get; set; }
    [StringLength(255)]
    public string? Name { get; set; }
    [NotEmptyGuid]
    public Guid? ErrorClassificationId { get; set; }
    public DateTime? UploadDate { get; set; }
    public string? FileLink { get; set; }
    [NotEmptyGuid]
    public Guid? FileTypeId { get; set; }
    [Range(typeof(long), "0", "9223372036854775807")]
    public long? FileSize { get; set; }
    [NotEmptyGuid]
    public Guid? UploadedBy { get; set; }
    public bool? Active { get; set; }
}
