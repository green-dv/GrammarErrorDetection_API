using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.SupportMaterials;

public sealed class SupportMaterialRecommendationQuery
{
    // Estudiante: se ignora y se usa el usuario autenticado. Docente y coordinador: obligatorio.
    [NotEmptyGuid]
    public Guid? UserId { get; set; }

    // Opcional: limita los errores a la inscripción aprobada del estudiante en ese curso.
    // Obligatorio para docentes, que solo ven a estudiantes de sus cursos.
    [NotEmptyGuid]
    public Guid? ClassId { get; set; }

    // Hasta 50: el plan de estudio muestra todas las categorías con errores del estudiante.
    [Range(1, 50)]
    public int CategoryLimit { get; set; } = 3;

    [Range(1, 10)]
    public int MaterialsPerCategory { get; set; } = 3;
}
