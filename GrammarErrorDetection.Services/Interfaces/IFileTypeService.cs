using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.FileTypes;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IFileTypeService
{
    Task<List<FileTypeEntity>> List(PaginationDto<FileTypeList> request);
    Task<FileTypeEntity> Get(Guid id);
}
