using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Classrooms
{
    public class ClassroomInsert
    {
        [Required]
        [StringLength(30)]
        public string Name { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public bool Active { get; set; } = true;
    }
}

