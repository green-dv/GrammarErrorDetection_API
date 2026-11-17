using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Registrations;

namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IRegistrationRepository
    {
        Task<List<RegistrationListItemDto>> ListSummary(PaginationDto<RegistrationSummaryList> request);
        /// <summary>Pagina y total sin paginar; por defecto el total es el de la pagina (los repositorios reales lo sobrescriben).</summary>
        async Task<PagedList<RegistrationListItemDto>> ListSummaryPage(PaginationDto<RegistrationSummaryList> request)
        {
            var items = await ListSummary(request);
            return new PagedList<RegistrationListItemDto>(items, items.Count);
        }
        Task<List<RegistrationEntity>> List(PaginationDto<RegistrationList> request);
        Task<RegistrationEntity> Insert(RegistrationInsert request);
        Task<RegistrationEntity> Update(RegistrationUpdate request);
    }
}

