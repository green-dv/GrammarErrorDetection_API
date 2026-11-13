using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Reports;

public sealed class StudentProgressQuery
{
    [NotEmptyGuid] public Guid? ClassId { get; set; }
    [NotEmptyGuid] public Guid? UserId { get; set; }
}

public sealed record StudentProgressDto(
    Guid UserId,
    string? StudentName,
    Guid ClassId,
    Guid RegistrationId,
    Guid ModuleNameId,
    string ModuleName,
    int Level,
    int Year,
    int Bimester,
    string? Parallel,
    decimal? Exam1Score,
    decimal? Exam2Score,
    decimal? FinalScore);
