using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.ModuleNames;

namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IModuleNameRepository
    {
        Task<List<ModuleNameEntity>> List(PaginationDto<ModuleNameList> request);
        Task<ModuleNameEntity> Insert(ModuleNameInsert request);
        Task<ModuleNameEntity> Update(ModuleNameUpdate request);
    }
}

