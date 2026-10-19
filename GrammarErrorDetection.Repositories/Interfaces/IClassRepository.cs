using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;

namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IClassRepository
    {
        Task<List<ClassEntity>> List(PaginationDto<ClassList> request);
        Task<ClassEntity> Insert(ClassInsert request);
        Task<ClassEntity> Update(ClassUpdate request);
    }
}

