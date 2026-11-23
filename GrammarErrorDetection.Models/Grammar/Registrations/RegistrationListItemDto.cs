namespace GrammarErrorDetection.Models.Grammar.Registrations;

public sealed class RegistrationListItemDto
{
    public Guid RegistrationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ClassId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CI { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ModuleName { get; set; }
    public Guid? ModuleId { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    /// <summary>False si el estudiante fue quitado del curso, trasladado o rechazado; null si el listado no lo informa.</summary>
    public bool? Active { get; set; }
}
