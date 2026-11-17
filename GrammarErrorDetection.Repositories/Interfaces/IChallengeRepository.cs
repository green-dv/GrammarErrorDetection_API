using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IChallengeRepository
{
    Task<List<ChallengeListItem>> List(PaginationDto<ChallengeList> request);
    /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina (los repositorios reales lo sobrescriben).</summary>
    async Task<PagedList<ChallengeListItem>> ListPage(PaginationDto<ChallengeList> request)
    {
        var items = await List(request);
        return new PagedList<ChallengeListItem>(items, items.Count);
    }
    Task<ChallengeEntity> Insert(ChallengeInsert request, Guid createdByUserId);
    Task<ChallengeEntity> Update(ChallengeUpdate request);
    // Nueva ronda con nuevas fechas: vuelve a habilitar el desafío para todos.
    Task<ChallengeEntity> Repeat(Guid id, ChallengeRepeat request);
    // Copia en borrador con las mismas condiciones.
    Task<ChallengeEntity> Duplicate(Guid id, Guid createdByUserId);
}
