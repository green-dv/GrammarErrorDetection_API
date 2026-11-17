using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Classes;

// Recibe los datos completos del curso y la lista final de estudiantes.
public sealed class ClassWithStudentsUpdate : ClassInsert, IValidatableObject
{
    [NotEmptyGuid]
    public Guid Id { get; set; }

    // Omitir la lista es un error; una lista vacía elimina todas las asignaciones.
    [Required]
    public List<Guid> StudentIds { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        ClassWithStudentsInsert.ValidateDetails(this, StudentIds);
}
