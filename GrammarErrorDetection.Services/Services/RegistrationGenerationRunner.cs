using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Classrooms;
using GrammarErrorDetection.Models.Grammar.Evaluations;
using GrammarErrorDetection.Models.Grammar.ModuleNames;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Repositories.Interfaces;

namespace GrammarErrorDetection.Services.Services;

// Coordina los CRUD existentes. Cada inserción confirma su propia operación.
internal sealed class RegistrationGenerationRunner(
    IRegistrationRepository registrations, IClassRepository classes,
    IClassroomRepository classrooms, IModuleNameRepository modules,
    IEvaluationRepository evaluations)
{
    internal sealed class Input
    {
        public List<ClassEntity> SourceClasses { get; init; } = [];
        public List<ClassEntity> TargetClasses { get; init; } = [];
        public List<ClassroomEntity> Classrooms { get; init; } = [];
        public List<RegistrationEntity> Registrations { get; init; } = [];
        public List<EvaluationEntity> Evaluations { get; init; } = [];
        public Dictionary<Guid, Guid?> NextModules { get; init; } = [];
    }

    internal async Task<Input> Load(RegistrationGenerationRequest request)
    {
        var sourceYear = request.Bimester == 1 ? request.Year - 1 : request.Year;
        var sourceBimester = request.Bimester == 1 ? 6 : request.Bimester - 1;
        var sourceClasses = await All(classes.List,
            new ClassList { Year = sourceYear, Bimester = sourceBimester, Active = true });
        var targetClasses = await All(classes.List,
            new ClassList { Year = request.Year, Bimester = request.Bimester, Active = true });
        var currentModules = await All(modules.List, new ModuleNameList { Active = true });
        var activeModuleIds = currentModules.Select(m => m.Id).ToHashSet();
        var input = new Input
        {
            SourceClasses = sourceClasses, TargetClasses = targetClasses,
            Classrooms = await All(classrooms.List, new ClassroomList { Active = true }),
            NextModules = currentModules.ToDictionary(m => m.Id,
                m => m.NextModuleId is Guid next && next != m.Id && activeModuleIds.Contains(next)
                    ? (Guid?)next : null)
        };
        foreach (var course in sourceClasses.Concat(targetClasses).DistinctBy(c => c.Id))
            input.Registrations.AddRange(await All(registrations.List,
                new RegistrationList { ClassId = course.Id, Active = true }));
        var sourceIds = sourceClasses.Select(c => c.Id).ToHashSet();
        foreach (var registration in input.Registrations.Where(r =>
                     r.ClassId.HasValue && sourceIds.Contains(r.ClassId.Value)))
            input.Evaluations.AddRange(await All(evaluations.List,
                new EvaluationList { RegistrationId = registration.Id, Active = true }));
        return input;
    }

    internal async Task<RegistrationGenerationResult> Save(
        RegistrationGenerationRequest request, Input input,
        IReadOnlySet<Guid> passedRegistrationIds, IReadOnlySet<Guid> failedRegistrationIds)
    {
        var plan = RegistrationGenerationPlanner.Build(request, input.SourceClasses,
            input.TargetClasses, input.Registrations, input.Classrooms, input.NextModules,
            passedRegistrationIds, failedRegistrationIds);
        var result = new RegistrationGenerationResult
        {
            SourceYear = request.Bimester == 1 ? request.Year - 1 : request.Year,
            SourceBimester = request.Bimester == 1 ? 6 : request.Bimester - 1,
            Skipped = plan.Skipped.Select(s => new RegistrationGenerationSkipped
            {
                RegistrationId = s.RegistrationId, UserId = s.UserId, Reason = s.Reason
            }).ToList()
        };
        foreach (var destination in plan.Classes)
        {
            Guid classId;
            if (destination.ExistingClassId is Guid existingClassId)
            {
                classId = existingClassId;
                result.ReusedClassIds.Add(classId);
            }
            else
            {
                var createdClass = await classes.Insert(destination.Class);
                classId = createdClass.Id;
                result.CreatedClassIds.Add(classId);
            }
            foreach (var studentId in destination.StudentIds)
            {
                var createdRegistration = await registrations.Insert(new RegistrationInsert
                {
                    UserId = studentId, ClassId = classId,
                    RegistrationDate = DateTime.UtcNow, Status = "Pending", Active = true
                });
                result.CreatedRegistrationIds.Add(createdRegistration.Id);
            }
        }
        result.Generated = true;
        return result;
    }

    private static async Task<List<TEntity>> All<TFilter, TEntity>(
        Func<PaginationDto<TFilter>, Task<List<TEntity>>> list, TFilter filter)
    {
        const int pageSize = 200;
        var entities = new List<TEntity>();
        for (var page = 1; ; page++)
        {
            var batch = await list(new PaginationDto<TFilter>
            {
                PageNumber = page, PageSize = pageSize, Filter = filter
            });
            entities.AddRange(batch);
            if (batch.Count < pageSize) return entities;
        }
    }
}
