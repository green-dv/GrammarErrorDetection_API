using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Validation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class NotEmptyGuidAttribute : ValidationAttribute
    {
        public NotEmptyGuidAttribute() : base("El campo {0} debe contener un UUID distinto de Guid.Empty.")
        {
        }

        public override bool IsValid(object? value)
        {
            return value is null || value is Guid guid && guid != Guid.Empty;
        }
    }
}
