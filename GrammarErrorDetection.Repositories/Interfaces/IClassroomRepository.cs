using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classrooms;

namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IClassroomRepository
    {
        Task<List<ClassroomEntity>> List(PaginationDto<ClassroomList> request);
        Task<ClassroomEntity> Insert(ClassroomInsert request);
        Task<ClassroomEntity> Update(ClassroomUpdate request);
    }
}

