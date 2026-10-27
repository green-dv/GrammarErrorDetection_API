using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.ErrorClassifications;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class ErrorClassificationService(IErrorClassificationRepository repository) : IErrorClassificationService
{
    public Task<List<ErrorClassificationEntity>> List(PaginationDto<ErrorClassificationList> request) => repository.List(request);

    public async Task<ErrorClassificationEntity> Get(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID no puede estar vacío.", nameof(id));
        var items = await repository.List(new PaginationDto<ErrorClassificationList>
        {
            PageNumber = 1, PageSize = 1, Filter = new ErrorClassificationList { Id = id }
        });
        return items.FirstOrDefault(item => item.Id == id)
            ?? throw new KeyNotFoundException("No se encontró el registro.");
    }
}
