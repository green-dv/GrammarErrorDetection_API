using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services
{
    public sealed partial class ClassService : IClassService
    {
        private readonly IClassRepository repository;
        private readonly IClassroomRepository classrooms;
        private readonly IRegistrationRepository registrations;
        private readonly IModuleNameRepository modules;
        private readonly IEvaluationRepository evaluations;
        private readonly IUserRepository users;
        private readonly IRoleRepository roles;

        public ClassService(IClassRepository repository, IClassroomRepository classrooms,
            IRegistrationRepository registrations, IModuleNameRepository modules,
            IEvaluationRepository evaluations, IUserRepository users, IRoleRepository roles)
        {
            ArgumentNullException.ThrowIfNull(repository);
            ArgumentNullException.ThrowIfNull(classrooms);
            ArgumentNullException.ThrowIfNull(registrations);
            ArgumentNullException.ThrowIfNull(modules);
            ArgumentNullException.ThrowIfNull(evaluations);
            ArgumentNullException.ThrowIfNull(users);
            ArgumentNullException.ThrowIfNull(roles);
            this.repository = repository;
            this.classrooms = classrooms;
            this.registrations = registrations;
            this.modules = modules;
            this.evaluations = evaluations;
            this.users = users;
            this.roles = roles;
        }

        public Task<List<ClassEntity>> List(PaginationDto<ClassList> request)
            => repository.List(request);

        public Task<ClassEntity> Insert(ClassInsert request)
            => repository.Insert(request);

        public Task<ClassEntity> Update(ClassUpdate request)
            => repository.Update(request);
    }
}

