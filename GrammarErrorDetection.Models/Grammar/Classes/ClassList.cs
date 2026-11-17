using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Classes
{
    public class ClassList
    {
        [NotEmptyGuid]
        public Guid? Id { get; set; }

        [NotEmptyGuid]
        public Guid? TeacherId { get; set; }

        [NotEmptyGuid]
        public Guid? ModuleNameId { get; set; }

        public int? Bimester { get; set; }

        public int? Year { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        [StringLength(10)]
        public string? Parallel { get; set; }

        [NotEmptyGuid]
        public Guid? ClassroomId { get; set; }

        public TimeOnly? StartTime { get; set; }

        public TimeOnly? EndTime { get; set; }

        public DateOnly? CreatedAt { get; set; }

        public bool? Active { get; set; }
    }
}

