namespace GrammarErrorDetection.Models.Grammar.Registrations;

public sealed class RegistrationGenerationResult
{
    public bool Generated { get; set; }
    public int SourceYear { get; set; }
    public int SourceBimester { get; set; }
    public List<Guid> CreatedClassIds { get; set; } = [];
    public List<Guid> ReusedClassIds { get; set; } = [];
    public List<Guid> CreatedRegistrationIds { get; set; } = [];
    public List<RegistrationGenerationSkipped> Skipped { get; set; } = [];
}

public sealed class RegistrationGenerationSkipped
{
    public Guid RegistrationId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
