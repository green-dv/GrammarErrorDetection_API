using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.FileTypes;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IFileTypeRepository
{
    Task<List<FileTypeEntity>> List(PaginationDto<FileTypeList> request);
    /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina (los repositorios reales lo sobrescriben).</summary>
    async Task<PagedList<FileTypeEntity>> ListPage(PaginationDto<FileTypeList> request)
    {
        var items = await List(request);
        return new PagedList<FileTypeEntity>(items, items.Count);
    }
}
