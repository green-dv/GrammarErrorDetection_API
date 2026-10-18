using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Roles
{
    public class RoleList
    {
        [NotEmptyGuid]
        public Guid? Id { get; set; }

        [StringLength(50)]
        public string? Name { get; set; }

        public bool? Active { get; set; }
    }
}
