using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.ModuleNames
{
    public class ModuleNameList
    {
        [NotEmptyGuid]
        public Guid? Id { get; set; }

        [StringLength(30)]
        public string? Name { get; set; }

        public int? Level { get; set; }

        [NotEmptyGuid]
        public Guid? NextModuleId { get; set; }

        public bool? Active { get; set; }
    }
}

