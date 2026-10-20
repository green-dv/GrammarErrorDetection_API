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
        private readonly IClassRepository classes;
        private readonly IClassroomRepository classrooms;
        private readonly IModuleNameRepository modules;
        private readonly IEvaluationRepository evaluations;

        public RegistrationService(IRegistrationRepository repository, IClassRepository classes,
            IClassroomRepository classrooms, IModuleNameRepository modules, IEvaluationRepository evaluations)
        {
            ArgumentNullException.ThrowIfNull(repository);
            ArgumentNullException.ThrowIfNull(classes);
            ArgumentNullException.ThrowIfNull(classrooms);
            ArgumentNullException.ThrowIfNull(modules);
            ArgumentNullException.ThrowIfNull(evaluations);
            this.repository = repository;
            this.classes = classes;
            this.classrooms = classrooms;
            this.modules = modules;
            this.evaluations = evaluations;
            generation = new RegistrationGenerationRunner(repository, classes, classrooms, modules, evaluations);
        }

        public Task<List<RegistrationListItemDto>> List(PaginationDto<RegistrationSummaryList> request)
            => repository.ListSummary(request);

        public Task<RegistrationEntity> Insert(RegistrationInsert request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (IsStatus(request.Status, "Rejected")) request.Active = false;
            return repository.Insert(request);
        }

        public async Task<RegistrationEntity> Update(RegistrationUpdate request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var current = await FindRegistration(request.Id);
            if (IsStatus(request.Status ?? current.Status, "Rejected")) request.Active = false;
            return await repository.Update(request);
        }
    }
}

