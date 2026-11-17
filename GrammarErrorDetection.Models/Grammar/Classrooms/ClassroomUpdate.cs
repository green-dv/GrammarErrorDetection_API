using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Classrooms
{
    public class ClassroomUpdate
    {
        [NotEmptyGuid]
        public Guid Id { get; set; }

        [StringLength(30)]
        public string? Name { get; set; }

        [Range(1, 1000, ErrorMessage = "La capacidad debe estar entre 1 y 1000.")]
        public int? Capacity { get; set; }

        public bool? Active { get; set; }
    }
}

