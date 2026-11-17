using GrammarErrorDetection.Models.APIRequest;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class GamificationPage<T> : PaginationDto<T>
{
    public GamificationPage() { PageNumber = 1; PageSize = 10; }
}
