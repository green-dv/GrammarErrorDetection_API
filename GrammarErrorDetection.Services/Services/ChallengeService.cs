using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class ChallengeService(IChallengeRepository repository) : IChallengeService
{
    public Task<List<ChallengeListItem>> List(PaginationDto<ChallengeList> request) => repository.List(request);
    public Task<ChallengeEntity> Insert(ChallengeInsert request, Guid createdByUserId) => repository.Insert(request, createdByUserId);
    public Task<ChallengeEntity> Update(ChallengeUpdate request) => repository.Update(request);
    public Task<ChallengeEntity> Repeat(Guid id, ChallengeRepeat request) => repository.Repeat(id, request);
    public Task<ChallengeEntity> Duplicate(Guid id, Guid createdByUserId) => repository.Duplicate(id, createdByUserId);
}
