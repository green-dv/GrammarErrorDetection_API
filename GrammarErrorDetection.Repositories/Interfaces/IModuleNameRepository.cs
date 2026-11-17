using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.Grammar.ModuleNames;

namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IModuleNameRepository
    {
        Task<List<ModuleNameEntity>> List(PaginationDto<ModuleNameList> request);
        /// <summary>Página y total sin paginar en una sola llamada (formato de dos DataTables).</summary>
        Task<PagedList<ModuleNameEntity>> ListPage(PaginationDto<ModuleNameList> request);
        Task<ModuleNameEntity> Insert(ModuleNameInsert request);
        Task<ModuleNameEntity> Update(ModuleNameUpdate request);
    }
}

