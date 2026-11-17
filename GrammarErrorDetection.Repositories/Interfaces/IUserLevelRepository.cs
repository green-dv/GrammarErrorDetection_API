using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.Grammar.UserLevels;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IUserLevelRepository
{
    Task<PagedList<UserLevelListItemDto>> List(PaginationDto<UserLevelList> request);
    Task<UserLevelListItemDto> Insert(UserLevelInsert request);
    Task<UserLevelListItemDto> Update(UserLevelUpdate request);
}
