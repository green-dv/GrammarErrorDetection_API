using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;

namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IClassRepository
    {
        Task<List<ClassEntity>> List(PaginationDto<ClassList> request);
        /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina (los repositorios reales lo sobrescriben).</summary>
        async Task<PagedList<ClassEntity>> ListPage(PaginationDto<ClassList> request)
        {
            var items = await List(request);
            return new PagedList<ClassEntity>(items, items.Count);
        }
        Task<ClassEntity> Insert(ClassInsert request);
        Task<ClassEntity> Update(ClassUpdate request);
    }
}

