using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Roles
{
    public class RoleInsert
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public bool Active { get; set; } = true;
    }
}
