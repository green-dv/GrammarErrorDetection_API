using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.ModuleNames
{
    public class ModuleNameInsert
    {
        [Required]
        [StringLength(30)]
        public string Name { get; set; } = string.Empty;

        public int Level { get; set; }

        [NotEmptyGuid]
        public Guid? NextModuleId { get; set; }

        public bool Active { get; set; } = true;
    }
}

