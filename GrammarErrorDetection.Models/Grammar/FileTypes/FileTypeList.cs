using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.FileTypes;

public sealed class FileTypeList
{
    [NotEmptyGuid]
    public Guid? Id { get; set; }
    [StringLength(50)]
    public string? Name { get; set; }
    [StringLength(20)]
    public string? Extension { get; set; }
    [StringLength(255)]
    public string? MimeType { get; set; }
    public bool? Active { get; set; }
    public DateOnly? CreatedAt { get; set; }
}
