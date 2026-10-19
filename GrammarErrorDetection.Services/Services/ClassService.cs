using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services
{
    public sealed class ClassService : IClassService
    {
        private readonly IClassRepository repository;

        public ClassService(IClassRepository repository)
        {
            ArgumentNullException.ThrowIfNull(repository);
            this.repository = repository;
        }

        public Task<List<ClassEntity>> List(PaginationDto<ClassList> request)
            => repository.List(request);

        public Task<ClassEntity> Insert(ClassInsert request)
            => repository.Insert(request);

        public Task<ClassEntity> Update(ClassUpdate request)
            => repository.Update(request);
    }
}

