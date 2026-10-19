namespace GrammarErrorDetection.Models.Grammar.Users;

public sealed class CourseSummaryDto
{
    public Guid ClassId { get; set; }
    public Guid ModuleNameId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Bimester { get; set; }
    public string? Parallel { get; set; }
    public string RegistrationStatus { get; set; } = string.Empty;
}
