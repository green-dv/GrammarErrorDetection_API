using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.FileTypes;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IFileTypeRepository
{
    Task<List<FileTypeEntity>> List(PaginationDto<FileTypeList> request);
}
