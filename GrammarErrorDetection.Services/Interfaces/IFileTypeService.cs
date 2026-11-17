using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.FileTypes;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IFileTypeService
{
    Task<List<FileTypeEntity>> List(PaginationDto<FileTypeList> request);
    async Task<PagedList<FileTypeEntity>> ListPage(PaginationDto<FileTypeList> request)
    {
        var items = await List(request);
        return new PagedList<FileTypeEntity>(items, items.Count);
    }
    Task<FileTypeEntity> Get(Guid id);
}
