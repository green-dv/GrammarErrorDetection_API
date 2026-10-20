using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Classrooms;
using GrammarErrorDetection.Models.Grammar.Registrations;

namespace GrammarErrorDetection.Services.Services;

// Plan en memoria: no escribe ni recibe notas desde el cliente.
// Los cupos incluyen las inscripciones existentes y las propuestas en este plan.
internal static class RegistrationGenerationPlanner
{
    internal sealed class PlannedClass
    {
        public Guid? ExistingClassId { get; init; }
        public required Guid SourceClassId { get; init; }
        public required ClassInsert Class { get; init; }
        public List<Guid> StudentIds { get; } = [];
        public int OccupiedSeats { get; set; }
    }

    internal sealed record SkippedStudent(Guid RegistrationId, Guid UserId, string Reason);

    internal sealed class Plan
    {
        public List<PlannedClass> Classes { get; } = [];
        public List<SkippedStudent> Skipped { get; } = [];
    }

    internal static Plan Build(
        RegistrationGenerationRequest request,
        IReadOnlyCollection<ClassEntity> sourceClasses,
        IReadOnlyCollection<ClassEntity> targetClasses,
        IReadOnlyCollection<RegistrationEntity> registrations,
        IReadOnlyCollection<ClassroomEntity> classrooms,
        IReadOnlyDictionary<Guid, Guid?> nextModules,
        IReadOnlySet<Guid> passedRegistrationIds,
        IReadOnlySet<Guid> failedRegistrationIds)
    {
        if (passedRegistrationIds.Overlaps(failedRegistrationIds))
            throw new ArgumentException("Una inscripción no puede estar aprobada y reprobada a la vez.");

        var sourceYear = request.Bimester == 1 ? request.Year - 1 : request.Year;
        var sourceBimester = request.Bimester == 1 ? 6 : request.Bimester - 1;
        var sources = sourceClasses.Where(c => c.Active && c.Year == sourceYear && c.Bimester == sourceBimester)
            .ToDictionary(c => c.Id);
        var targets = targetClasses.Where(c => c.Active && c.Year == request.Year && c.Bimester == request.Bimester)
            .OrderBy(c => c.Id).ToList();
        var rooms = classrooms.Where(c => c.Active).ToDictionary(c => c.Id);
        var sourceRegistrations = registrations.Where(r => r.Active == true && r.ClassId.HasValue
                && sources.ContainsKey(r.ClassId.Value) && ReservesSeat(r))
            .OrderBy(r => r.ClassId).ThenBy(r => r.UserId).ThenBy(r => r.Id).ToList();
        var targetIds = targets.Select(c => c.Id).ToHashSet();
        var targetRegistrations = registrations.Where(r => r.Active == true && r.ClassId.HasValue
            && targetIds.Contains(r.ClassId.Value) && ReservesSeat(r)).ToList();
        var plan = new Plan();
        foreach (var target in targets)
        {
            var targetData = Copy(target, target.ModuleNameId, request);
            targetData.StartDate = target.StartDate;
            targetData.EndDate = target.EndDate;
            plan.Classes.Add(new PlannedClass
            {
                ExistingClassId = target.Id,
                SourceClassId = Guid.Empty,
                Class = targetData,
                OccupiedSeats = targetRegistrations.Count(r => r.ClassId == target.Id)
            });
        }

        // Evita dos inscripciones para el mismo estudiante y módulo en el periodo destino.
        var enrolled = targetRegistrations.Select(r => (r.UserId,
            targets.Single(c => c.Id == r.ClassId).ModuleNameId)).ToHashSet();

        // Primero los aprobados: una clase por grupo de origen y siguiente módulo.
        foreach (var group in sourceRegistrations.Where(r => passedRegistrationIds.Contains(r.Id))
                     .GroupBy(r => r.ClassId!.Value))
        {
            var source = sources[group.Key];
            if (!nextModules.TryGetValue(source.ModuleNameId, out var nextModule) || nextModule is null || nextModule == Guid.Empty)
            {
                foreach (var registration in group)
                    Skip(registration, "Sin siguiente módulo configurado (o fin de programa).");
                continue;
            }

            var pending = group.Where(r => !AlreadyEnrolled(r, nextModule.Value)).ToList();
            if (pending.Count == 0)
                continue;
            var destination = plan.Classes.FirstOrDefault(c => SameParameters(c.Class, source, nextModule.Value));
            destination ??= Create(source, nextModule.Value);
            foreach (var registration in pending)
            {
                if (AlreadyEnrolled(registration, nextModule.Value))
                    continue;
                if (destination is null || !HasSpace(destination))
                    Skip(registration, "No se pudo conservar la clase de origen con cupo y recursos disponibles.");
                else
                    Enroll(registration, destination);
            }
        }

        // Después los reprobados: conservan el módulo, priorizando el horario más cercano.
        foreach (var registration in sourceRegistrations.Where(r => failedRegistrationIds.Contains(r.Id)))
        {
            var source = sources[registration.ClassId!.Value];
            if (AlreadyEnrolled(registration, source.ModuleNameId))
                continue;
            var destination = plan.Classes.Where(c => c.Class.ModuleNameId == source.ModuleNameId && HasSpace(c))
                .OrderBy(c => ScheduleDistance(c.Class, source))
                .ThenBy(c => c.ExistingClassId).ThenBy(c => c.SourceClassId).FirstOrDefault();
            // Si no queda una clase disponible, intenta abrir otra con los recursos de origen.
            destination ??= Create(source, source.ModuleNameId);
            if (destination is null || !HasSpace(destination))
                Skip(registration, "Sin clase del mismo módulo con cupo y recursos disponibles.");
            else
                Enroll(registration, destination);
        }

        foreach (var registration in sourceRegistrations.Where(r =>
                     !passedRegistrationIds.Contains(r.Id) && !failedRegistrationIds.Contains(r.Id)))
            Skip(registration, "Sin nota final: no se clasifica como reprobado.");

        // No propone crear clases vacías ni modificar clases existentes sin nuevas inscripciones.
        plan.Classes.RemoveAll(c => c.StudentIds.Count == 0);
        return plan;

        bool HasSpace(PlannedClass candidate) => rooms.TryGetValue(candidate.Class.ClassroomId, out var room)
            && candidate.OccupiedSeats < room.Capacity;

        PlannedClass? Create(ClassEntity source, Guid moduleId)
        {
            if (!rooms.TryGetValue(source.ClassroomId, out var room) || room.Capacity <= 0)
                return null;
            var proposed = Copy(source, moduleId, request);
            // No inventa otro profesor/aula ni permite superposición de sus horarios.
            if (plan.Classes.Any(c => Conflicts(c.Class, proposed)))
                return null;
            var created = new PlannedClass { SourceClassId = source.Id, Class = proposed };
            plan.Classes.Add(created);
            return created;
        }

        bool AlreadyEnrolled(RegistrationEntity registration, Guid moduleId)
        {
            if (!enrolled.Contains((registration.UserId, moduleId)))
                return false;
            Skip(registration, "El estudiante ya tiene inscripción para el módulo en el periodo destino.");
            return true;
        }

        void Enroll(RegistrationEntity registration, PlannedClass destination)
        {
            destination.StudentIds.Add(registration.UserId);
            destination.OccupiedSeats++;
            enrolled.Add((registration.UserId, destination.Class.ModuleNameId));
        }

        void Skip(RegistrationEntity registration, string reason)
            => plan.Skipped.Add(new SkippedStudent(registration.Id, registration.UserId, reason));
    }

    private static bool ReservesSeat(RegistrationEntity registration)
        => !string.Equals(registration.Status, "Rejected", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(registration.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);

    private static double ScheduleDistance(ClassInsert candidate, ClassEntity source)
        => Math.Abs((candidate.StartTime - source.StartTime).TotalMinutes)
            + Math.Abs((candidate.EndTime - source.EndTime).TotalMinutes);

    private static bool SameParameters(ClassInsert candidate, ClassEntity source, Guid moduleId)
        => candidate.ModuleNameId == moduleId && candidate.TeacherId == source.TeacherId
            && candidate.ClassroomId == source.ClassroomId && candidate.Parallel == source.Parallel
            && candidate.StartTime == source.StartTime && candidate.EndTime == source.EndTime;

    private static bool Conflicts(ClassInsert left, ClassInsert right)
        => (left.ClassroomId == right.ClassroomId || left.TeacherId == right.TeacherId)
            && (left.StartDate is null || right.EndDate is null || left.StartDate <= right.EndDate)
            && (right.StartDate is null || left.EndDate is null || right.StartDate <= left.EndDate)
            && left.StartTime < right.EndTime && right.StartTime < left.EndTime;

    private static ClassInsert Copy(ClassEntity source, Guid moduleId, RegistrationGenerationRequest request)
        => new()
        {
            TeacherId = source.TeacherId,
            ModuleNameId = moduleId,
            Bimester = request.Bimester,
            Year = request.Year,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Parallel = source.Parallel,
            ClassroomId = source.ClassroomId,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            Active = true
        };
}
