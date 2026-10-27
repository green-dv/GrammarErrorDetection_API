namespace GrammarErrorDetection.Models.Grammar.Users
{
    public class UserEntity
    {
        public Guid Id { get; set; }
        public DateOnly? FechaNacimiento { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string CI { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public Guid RoleId { get; set; }

        public bool HasAuthenticator { get; set; }

        public string? TotpSecret { get; set; }

        public bool IsTotpEnabled { get; set; }

        public bool ChangePassword { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool Active { get; set; } = true;

        // Datos adicionales de User_List; no pertenecen al compuesto de Insert/Update.
        public string RoleName { get; set; } = string.Empty;
        public CurrentCourseDto? CurrentCourse { get; set; }
        public decimal? CurrentCourseFinalScore { get; set; }
        public CourseSummaryDto? LastCourse { get; set; }
    }
}
