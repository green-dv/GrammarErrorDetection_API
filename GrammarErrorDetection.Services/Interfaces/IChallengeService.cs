using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IChallengeService
{
    Task<List<ChallengeListItem>> List(PaginationDto<ChallengeList> request);
    async Task<PagedList<ChallengeListItem>> ListPage(PaginationDto<ChallengeList> request)
    {
        var items = await List(request);
        return new PagedList<ChallengeListItem>(items, items.Count);
    }
    Task<ChallengeEntity> Insert(ChallengeInsert request, Guid createdByUserId);
    Task<ChallengeEntity> Update(ChallengeUpdate request);
    Task<ChallengeEntity> Repeat(Guid id, ChallengeRepeat request);
    Task<ChallengeEntity> Duplicate(Guid id, Guid createdByUserId);
}
