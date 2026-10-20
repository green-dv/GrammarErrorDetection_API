namespace GrammarErrorDetection.Models.Grammar.Classrooms
{
    public class ClassroomEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool Active { get; set; } = true;
    }
}

