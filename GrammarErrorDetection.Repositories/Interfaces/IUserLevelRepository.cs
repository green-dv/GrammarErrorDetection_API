using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.UserLevels;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IUserLevelRepository
{
    Task<List<UserLevelListItemDto>> List(PaginationDto<UserLevelList> request);
}
