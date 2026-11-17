using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.UserLevels;

/// <summary>Solo se actualizan los campos enviados; Active = false deshabilita el nivel.</summary>
public sealed class UserLevelUpdate
{
    [NotEmptyGuid]
    public Guid Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El nivel debe ser un entero positivo.")]
    public int? Level { get; set; }

    [StringLength(50, MinimumLength = 1)]
    public string? Name { get; set; }

    [Range(0, long.MaxValue, ErrorMessage = "Los puntos mínimos no pueden ser negativos.")]
    public long? MinPoints { get; set; }

    public bool? Active { get; set; }
}
