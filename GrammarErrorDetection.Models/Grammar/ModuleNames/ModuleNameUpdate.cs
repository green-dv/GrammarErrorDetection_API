using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.ModuleNames
{
    public class ModuleNameUpdate : IValidatableObject
    {
        [NotEmptyGuid]
        public Guid Id { get; set; }

        [StringLength(30)]
        public string? Name { get; set; }

        public int? Level { get; set; }

        [NotEmptyGuid]
        public Guid? NextModule { get; set; }

        // Null conserva el valor actual, como los demás campos del Update.
        public bool ClearNextModule { get; set; }

        public bool? Active { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (NextModule == Id)
                yield return new ValidationResult("El siguiente módulo debe ser diferente al actual.", [nameof(NextModule)]);
            if (ClearNextModule && NextModule.HasValue)
                yield return new ValidationResult("No se puede asignar y quitar el siguiente módulo a la vez.",
                    [nameof(NextModule), nameof(ClearNextModule)]);
        }
    }
}

