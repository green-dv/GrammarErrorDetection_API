using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Users
{
    public class UserList
    {
        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        [StringLength(15)]
        public string? CI { get; set; }

        [StringLength(12)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        public DateOnly? CreatedAt { get; set; }

        [NotEmptyGuid]
        public Guid? RoleId { get; set; }

        public bool? Active { get; set; }
    }
}
