using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.UserLevels;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IUserLevelService
{
    Task<List<UserLevelListItemDto>> List(PaginationDto<UserLevelList> request);
}
