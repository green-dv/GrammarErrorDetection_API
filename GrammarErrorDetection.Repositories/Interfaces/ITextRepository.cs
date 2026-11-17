using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Texts;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface ITextRepository
{
    Task<List<TextListItemDto>> List(PaginationDto<TextList> request);
    /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina (los repositorios reales lo sobrescriben).</summary>
    async Task<PagedList<TextListItemDto>> ListPage(PaginationDto<TextList> request)
    {
        var items = await List(request);
        return new PagedList<TextListItemDto>(items, items.Count);
    }
    Task<TextEntity> Insert(TextInsert request);
    Task<TextEntity> CompleteAnalysis(Guid id, Guid? registrationId);
    Task<TextEntity> Update(TextUpdate request);
}
