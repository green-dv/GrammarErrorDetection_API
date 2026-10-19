using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Classrooms
{
    public class ClassroomList
    {
        [NotEmptyGuid]
        public Guid? Id { get; set; }

        [StringLength(30)]
        public string? Name { get; set; }

        public int? Capacity { get; set; }

        public DateOnly? CreatedAt { get; set; }

        public bool? Active { get; set; }
    }
}

