using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Classes;

public sealed class ClassWithStudentsInsert : ClassInsert, IValidatableObject
{
    [Required]
    [MinLength(1)]
    public List<Guid> StudentIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => ValidateDetails(this, StudentIds);

    internal static IEnumerable<ValidationResult> ValidateDetails(ClassInsert request, List<Guid>? studentIds)
    {
        if (request.Year is < 1 or > 9999 || request.Bimester is < 1 or > 6)
            yield return new ValidationResult("Indicar un año y bimestre válidos.", [nameof(Year), nameof(Bimester)]);
        if (request.StartDate is null || request.EndDate is null || request.EndDate < request.StartDate
            || request.StartDate.Value.Year != request.Year || request.EndDate.Value.Year != request.Year)
            yield return new ValidationResult("Indicar fechas ordenadas y pertenecientes al año de la clase.", [nameof(StartDate), nameof(EndDate)]);
        if (request.StartTime >= request.EndTime)
            yield return new ValidationResult("La hora final debe ser posterior a la inicial.", [nameof(EndTime)]);
        if (!request.Active)
            yield return new ValidationResult("La clase debe estar activa para asignar estudiantes.", [nameof(Active)]);
        if (studentIds is not null && (studentIds.Contains(Guid.Empty) || studentIds.Count != studentIds.Distinct().Count()))
            yield return new ValidationResult("Los estudiantes deben tener UUID válidos y no repetirse.", [nameof(StudentIds)]);
    }
}
