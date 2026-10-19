using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classrooms;

namespace GrammarErrorDetection.Services.Interfaces
{
    public interface IClassroomService
    {
        Task<List<ClassroomEntity>> List(PaginationDto<ClassroomList> request);
        Task<ClassroomEntity> Insert(ClassroomInsert request);
        Task<ClassroomEntity> Update(ClassroomUpdate request);
    }
}

