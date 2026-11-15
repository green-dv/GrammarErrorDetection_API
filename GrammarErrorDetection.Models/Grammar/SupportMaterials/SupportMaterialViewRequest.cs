using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.SupportMaterials;

public sealed class SupportMaterialViewRequest
{
    // Opcional: inscripción (curso) desde la que se abrió el material. Debe pertenecer al estudiante.
    [NotEmptyGuid]
    public Guid? RegistrationId { get; set; }

    // true cuando el estudiante descargó o abrió el enlace externo, no solo la vista previa.
    public bool Downloaded { get; set; }
}
