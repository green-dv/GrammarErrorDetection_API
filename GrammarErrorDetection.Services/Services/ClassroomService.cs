using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classrooms;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services
{
    public sealed class ClassroomService : IClassroomService
    {
        private readonly IClassroomRepository repository;

        public ClassroomService(IClassroomRepository repository)
        {
            ArgumentNullException.ThrowIfNull(repository);
            this.repository = repository;
        }

        public Task<List<ClassroomEntity>> List(PaginationDto<ClassroomList> request)
            => repository.List(request);

        public Task<ClassroomEntity> Insert(ClassroomInsert request)
            => repository.Insert(request);

        public Task<ClassroomEntity> Update(ClassroomUpdate request)
            => repository.Update(request);
    }
}

