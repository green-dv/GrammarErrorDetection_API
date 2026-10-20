namespace GrammarErrorDetection.Models.Grammar.Classes
{
    public class ClassEntity
    {
        public Guid Id { get; set; }

        public Guid TeacherId { get; set; }

        public Guid ModuleNameId { get; set; }

        public int Bimester { get; set; }

        public int Year { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? Parallel { get; set; }

        public Guid ClassroomId { get; set; }

        public TimeOnly StartTime { get; set; } = new(8, 0);

        public TimeOnly EndTime { get; set; } = new(10, 0);

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool Active { get; set; } = true;

        // Campos adicionales del cursor de Class_List.
        public string? TeacherName { get; set; }
        public string? ModuleName { get; set; }
        public int? ModuleLevel { get; set; }
        public string? ClassroomName { get; set; }
        public int? ClassroomCapacity { get; set; }
        public long? StudentCount { get; set; }
        public decimal? Exam1Average { get; set; }
        public decimal? Exam2Average { get; set; }
    }
}

