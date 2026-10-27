using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class SupportMaterialService : ISupportMaterialService
{
    private readonly ISupportMaterialRepository repository;

    public SupportMaterialService(ISupportMaterialRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        this.repository = repository;
    }

    public Task<List<SupportMaterialListItemDto>> List(PaginationDto<SupportMaterialList> request) => repository.List(request);

    public async Task<SupportMaterialListItemDto> Get(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("El identificador de material no puede estar vacío.", nameof(id));
        var materials = await repository.List(new PaginationDto<SupportMaterialList>
        {
            PageNumber = 1, PageSize = 1, Filter = new SupportMaterialList { Id = id }
        });
        return materials.SingleOrDefault(item => item.Id == id)
            ?? throw new KeyNotFoundException("No se encontró el material de apoyo.");
    }

    public Task<SupportMaterialEntity> Insert(SupportMaterialInsert request) => repository.Insert(request);
    public Task<SupportMaterialEntity> Update(SupportMaterialUpdate request) => repository.Update(request);
}
