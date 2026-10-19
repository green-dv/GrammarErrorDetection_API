using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services
{
    public sealed partial class RegistrationService : IRegistrationService
    {
        private readonly IRegistrationRepository repository;
        private readonly RegistrationGenerationRunner generation;

        public RegistrationService(IRegistrationRepository repository, IClassRepository classes,
            IClassroomRepository classrooms, IModuleNameRepository modules, IEvaluationRepository evaluations)
        {
            ArgumentNullException.ThrowIfNull(repository);
            ArgumentNullException.ThrowIfNull(classes);
            ArgumentNullException.ThrowIfNull(classrooms);
            ArgumentNullException.ThrowIfNull(modules);
            ArgumentNullException.ThrowIfNull(evaluations);
            this.repository = repository;
            generation = new RegistrationGenerationRunner(repository, classes, classrooms, modules, evaluations);
        }

        public Task<List<RegistrationEntity>> List(PaginationDto<RegistrationList> request)
            => repository.List(request);

        public Task<RegistrationEntity> Insert(RegistrationInsert request)
            => repository.Insert(request);

        public Task<RegistrationEntity> Update(RegistrationUpdate request)
            => repository.Update(request);
    }
}

