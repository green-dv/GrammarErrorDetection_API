using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.UserLevels;

public sealed class UserLevelInsert
{
    [Range(1, int.MaxValue, ErrorMessage = "El nivel debe ser un entero positivo.")]
    public int Level { get; set; }

    [Required] [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Range(0, long.MaxValue, ErrorMessage = "Los puntos mínimos no pueden ser negativos.")]
    public long MinPoints { get; set; }

    public bool Active { get; set; } = true;
}
