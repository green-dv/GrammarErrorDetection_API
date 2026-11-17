using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Classrooms
{
    public class ClassroomInsert
    {
        [Required]
        [StringLength(30)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 1000, ErrorMessage = "La capacidad debe estar entre 1 y 1000.")]
        public int Capacity { get; set; }

        public bool Active { get; set; } = true;
    }
}

