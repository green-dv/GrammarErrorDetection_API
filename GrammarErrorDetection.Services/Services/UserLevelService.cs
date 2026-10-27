using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.UserLevels;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class UserLevelService(IUserLevelRepository repository) : IUserLevelService
{
    public Task<List<UserLevelListItemDto>> List(PaginationDto<UserLevelList> request) => repository.List(request);
}
