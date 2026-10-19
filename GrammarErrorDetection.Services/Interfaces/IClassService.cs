using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface IClassService
    {
        Task<List<ClassEntity>> List(PaginationDto<ClassList> request);
        Task<ClassEntity> Insert(ClassInsert request);
        Task<ClassEntity> Update(ClassUpdate request);
    }
}

