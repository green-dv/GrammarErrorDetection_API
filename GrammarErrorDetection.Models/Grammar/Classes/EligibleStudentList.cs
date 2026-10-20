using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Classes;

public sealed class EligibleStudentList
{
    [NotEmptyGuid]
    public Guid? ClassId { get; set; }

    [NotEmptyGuid]
    public Guid ModuleNameId { get; set; }
    [Range(1, 9999)]
    public int Year { get; set; }
    [Range(1, 6)]
    public int Bimester { get; set; }
    [StringLength(50)]
    public string? FirstName { get; set; }
    [StringLength(50)]
    public string? LastName { get; set; }
    [StringLength(15)]
    public string? CI { get; set; }
    public bool? IsAssigned { get; set; }
}
