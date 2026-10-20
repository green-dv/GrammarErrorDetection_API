namespace GrammarErrorDetection.Models.Grammar.Classes;

public sealed class ClassWithStudentsResult
{
    public ClassEntity Class { get; set; } = new();
    public List<StudentClassAssignmentResult> Students { get; set; } = [];
    public List<Guid> RemovedRegistrationIds { get; set; } = [];
}

public sealed class StudentClassAssignmentResult
{
    public Guid UserId { get; set; }
    public Guid RegistrationId { get; set; }
    public Guid? DeactivatedRegistrationId { get; set; }
    public string Status { get; set; } = string.Empty;
}
