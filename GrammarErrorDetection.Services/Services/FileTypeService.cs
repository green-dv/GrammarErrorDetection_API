using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.FileTypes;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class FileTypeService(IFileTypeRepository repository) : IFileTypeService
{
    public Task<List<FileTypeEntity>> List(PaginationDto<FileTypeList> request) => repository.List(request);

    public async Task<FileTypeEntity> Get(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID no puede estar vacío.", nameof(id));
        var items = await repository.List(new PaginationDto<FileTypeList>
        {
            PageNumber = 1, PageSize = 1, Filter = new FileTypeList { Id = id }
        });
        return items.FirstOrDefault(item => item.Id == id)
            ?? throw new KeyNotFoundException("No se encontró el registro.");
    }
}
