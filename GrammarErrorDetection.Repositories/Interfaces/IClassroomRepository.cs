using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.Grammar.Classrooms;

namespace GrammarErrorDetection.Repositories.Interfaces
{
    public interface IClassroomRepository
    {
        Task<List<ClassroomEntity>> List(PaginationDto<ClassroomList> request);
        /// <summary>Página y total sin paginar en una sola llamada (formato de dos DataTables).</summary>
        Task<PagedList<ClassroomEntity>> ListPage(PaginationDto<ClassroomList> request);
        Task<ClassroomEntity> Insert(ClassroomInsert request);
        Task<ClassroomEntity> Update(ClassroomUpdate request);
    }
}

