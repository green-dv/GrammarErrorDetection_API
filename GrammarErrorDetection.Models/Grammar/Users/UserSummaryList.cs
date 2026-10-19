using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Grammar.Users;

public sealed class UserSummaryList : UserList
{
    // Periodo consultado: determina qué curso es actual y cuál es anterior.
    [Range(1, 9999)]
    public int Year { get; set; }

    [Range(1, 6)]
    public int Bimester { get; set; }

    public bool? IsEnrolled { get; set; }
}
