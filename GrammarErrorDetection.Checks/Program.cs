using System.ComponentModel.DataAnnotations;
using System.Reflection;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Classrooms;
using GrammarErrorDetection.Models.Grammar.Evaluations;
using GrammarErrorDetection.Models.Grammar.ModuleNames;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Services;

var checks = 0;
var student = Guid.NewGuid();
var room = new ClassroomEntity { Id = Guid.NewGuid(), Name = "Aula 1", Capacity = 2 };
var next = new ModuleNameEntity { Id = Guid.NewGuid(), Name = "Módulo II" };
var first = new ModuleNameEntity { Id = Guid.NewGuid(), Name = "Módulo I", NextModule = next.Id };
var source = Course(first.Id, 2029, 6);
var repeat = Course(first.Id, 2030, 1);
var advance = Course(next.Id, 2030, 1);
var alternative = Course(next.Id, 2030, 1);
var classes = new List<ClassEntity> { source, repeat, advance, alternative };
var registrations = new Registrations();
var grades = new List<EvaluationEntity>();
var service = new RegistrationService(registrations,
    ListProxy.Create<IClassRepository, ClassList, ClassEntity>(classes),
    ListProxy.Create<IClassroomRepository, ClassroomList, ClassroomEntity>([room]),
    ListProxy.Create<IModuleNameRepository, ModuleNameList, ModuleNameEntity>([first, next]),
    ListProxy.Create<IEvaluationRepository, EvaluationList, EvaluationEntity>(grades));

var available = await Available();
Check(available.Count == 1 && available[0].ClassId == repeat.Id, "Un estudiante nuevo solo puede elegir módulos de entrada.");
Check(available[0].ClassroomName == room.Name && available[0].ModuleName == first.Name
    && available[0].StartTime == repeat.StartTime && available[0].Parallel == "A", "El listado incluye aula, módulo, paralelo y horario.");
var previous = new RegistrationEntity { Id = Guid.NewGuid(), UserId = student, ClassId = source.Id, Status = "Approved" };
registrations.Data.Add(previous);
Check((await Available()).Count == 0, "No permite avanzar ni repetir con notas incompletas.");
grades.AddRange([Grade(1, 70), Grade(2, 70)]);
Check((await Available()).Select(c => c.ClassId).ToHashSet().SetEquals([advance.Id, alternative.Id]), "70 puntos habilitan exclusivamente NextModule.");
grades[0].WritingScore--;
Check((await Available()).Single().ClassId == repeat.Id, "69.5 puntos habilitan el mismo módulo.");
grades[0].WritingScore++;
var application = await service.Submit(student, new() { ClassId = advance.Id, InvoiceLink = "invoice" });
var originalId = application.Id;
application = await service.Submit(student, new() { ClassId = alternative.Id });
Check(application.Id == originalId && registrations.Data.Count == 2 && application.ClassId == alternative.Id
    && application.InvoiceLink == "invoice", "Editar una solicitud pendiente conserva ID y factura sin duplicarla.");
await MustReject(() => service.Submit(student, new() { ClassId = repeat.Id }), "No permite postular directamente a un módulo no elegible.");
await service.Decide(originalId, new() { Approved = false });
Check(application.Active == false && application.Status == "Rejected"
    && !registrations.Data.Any(r => r.ClassId == alternative.Id && r.Active == true), "Rechazar desactiva la inscripción y elimina su pertenencia al curso.");
application = await service.Submit(student, new() { ClassId = advance.Id });
Check(application.Id == originalId && application.Active == true && application.Status == "Pending", "Una solicitud rechazada se reutiliza al volver a postular.");
await service.Decide(originalId, new() { Approved = true });
Check(application.Active == true && application.Status == "Approved", "El coordinador puede aprobar una pendiente válida.");
Check((await Available()).Count == 0, "Una inscripción aprobada oculta todos los cursos del periodo.");
await MustReject(() => service.Submit(student, new() { ClassId = alternative.Id }), "Una inscripción aprobada impide otra solicitud en el periodo.");
await MustReject(() => service.Decide(originalId, new() { Approved = false }), "Una decisión no se puede repetir sobre una inscripción aprobada.");
application.Status = "Pending";
await MustReject(() => service.Decide(originalId, new()), "Omitir la decisión no rechaza accidentalmente al estudiante.");
var outsiders = Enumerable.Range(0, 2).Select(_ => new RegistrationEntity
{
    Id = Guid.NewGuid(), UserId = Guid.NewGuid(), ClassId = advance.Id
}).ToList();
registrations.Data.AddRange(outsiders);
Check(!(await Available()).Any(c => c.ClassId == advance.Id), "Los cursos llenos no aparecen en el listado.");
await MustReject(() => service.Decide(originalId, new() { Approved = true }), "La aprobación comprueba nuevamente el cupo.");
await MustReject(() => service.Submit(student, new() { ClassId = advance.Id }), "El envío comprueba nuevamente el cupo.");
registrations.Data.RemoveAll(r => outsiders.Contains(r));
room.Active = false;
Check((await Available()).Count == 0, "Un aula inactiva no ofrece cursos.");
room.Active = true;
advance.EndDate = new DateTime(2020, 1, 1);
Check(!(await Available()).Any(c => c.ClassId == advance.Id), "Un curso terminado no se ofrece.");
advance.EndDate = null;
next.Active = false;
Check((await Available()).Count == 0, "Un módulo inactivo no se ofrece.");
next.Active = true;
var own = await service.ListMine(student, new() { PageNumber = 1, PageSize = 20, Filter = new() { UserId = Guid.NewGuid() } });
Check(own.Count == 2 && registrations.LastSummaryUserId == student, "El listado propio impone el usuario autenticado.");
await service.Update(new() { Id = application.Id, Status = "Rejected", Active = true });
Check(application.Active == false, "El CRUD previo también desactiva al rechazar.");
await service.Update(new() { Id = application.Id, Active = true });
Check(application.Active == false, "No permite reactivar una rechazada conservando su estado.");
previous.Active = false;
registrations.Data.Clear();
var unassigned = new RegistrationEntity { Id = Guid.NewGuid(), UserId = student };
registrations.Data.Add(unassigned);
application = await service.Submit(student, new() { ClassId = repeat.Id });
Check(application.Id == unassigned.Id && registrations.Data.Count == 1, "Reutiliza una inscripción pendiente sin curso.");
registrations.Data.Add(new RegistrationEntity { Id = Guid.NewGuid(), UserId = student, ClassId = repeat.Id });
await MustReject(() => service.Submit(student, new() { ClassId = repeat.Id }), "Rechaza ambigüedad si ya hay varias pendientes en el periodo.");
Console.WriteLine($"{checks} comprobaciones de inscripciones correctas.");
await RegistrationListingChecks.Run();
await CourseHistoryChecks.Run();
await EvaluationUserChecks.Run();

ClassEntity Course(Guid module, int year, int bimester) => new()
{
    Id = Guid.NewGuid(), ModuleNameId = module, ClassroomId = room.Id,
    Year = year, Bimester = bimester, Parallel = "A", StartTime = new(8, 0), EndTime = new(10, 0)
};
EvaluationEntity Grade(int period, int score) => new()
{
    Id = Guid.NewGuid(), RegistrationId = previous.Id, Period = period,
    WritingScore = score - 40, OralScore = 10, ListeningScore = 5, ReadingScore = 5,
    GrammarScore = 5, VocabularyScore = 0, AcademicPerformance = 15
};
Task<List<AvailableRegistrationCourseDto>> Available() => service.ListAvailableCourses(student,
    new() { PageNumber = 1, PageSize = 20, Filter = new() { Year = 2030, Bimester = 1 } });
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
async Task MustReject(Func<Task> action, string message)
{
    try { await action(); }
    catch (ValidationException) { checks++; return; }
    throw new Exception(message);
}

public class ListProxy : DispatchProxy
{
    public Func<object, object> List { get; set; } = null!;
    public static TRepository Create<TRepository, TFilter, TEntity>(List<TEntity> data) where TRepository : class
    {
        var proxy = Create<TRepository, ListProxy>();
        ((ListProxy)(object)proxy).List = request =>
        {
            var page = (PaginationDto<TFilter>)request;
            return Task.FromResult(data.Skip((page.PageNumber - 1) * page.PageSize).Take(page.PageSize).ToList());
        };
        return proxy;
    }
    protected override object? Invoke(MethodInfo? method, object?[]? args) =>
        method?.Name == "List" ? List(args![0]!) : throw new NotSupportedException();
}

internal sealed class Registrations : IRegistrationRepository
{
    public List<RegistrationEntity> Data { get; } = [];
    public Guid? LastSummaryUserId { get; private set; }
    public async Task<List<RegistrationListItemDto>> ListSummary(PaginationDto<RegistrationSummaryList> request)
    {
        LastSummaryUserId = request.Filter?.UserId;
        var records = await List(new PaginationDto<RegistrationList>
        {
            PageNumber = request.PageNumber, PageSize = request.PageSize,
            Filter = new RegistrationList { UserId = request.Filter?.UserId, Status = request.Filter?.Status }
        });
        return records.Select(r => new RegistrationListItemDto
        {
            RegistrationId = r.Id,
            UserId = r.UserId, ClassId = r.ClassId,
            FirstName = "Estudiante", LastName = "Prueba", CI = "123",
            RegistrationDate = r.RegistrationDate, Status = r.Status, UpdatedAt = r.UpdatedAt
        }).ToList();
    }
    public Task<List<RegistrationEntity>> List(PaginationDto<RegistrationList> request)
    {
        var f = request.Filter;
        return Task.FromResult(Data.Where(r => (!f?.Id.HasValue ?? true) || r.Id == f!.Id)
            .Where(r => (!f?.UserId.HasValue ?? true) || r.UserId == f!.UserId)
            .Where(r => (!f?.Active.HasValue ?? true) || r.Active == f!.Active)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList());
    }
    public Task<RegistrationEntity> Insert(RegistrationInsert request)
    {
        var entity = new RegistrationEntity
        {
            Id = Guid.NewGuid(), UserId = request.UserId, ClassId = request.ClassId,
            Status = request.Status, Active = request.Active, InvoiceLink = request.InvoiceLink,
            RegistrationDate = request.RegistrationDate, CreatedAt = DateTime.UtcNow
        };
        Data.Add(entity);
        return Task.FromResult(entity);
    }
    public Task<RegistrationEntity> Update(RegistrationUpdate request)
    {
        var entity = Data.Single(r => r.Id == request.Id);
        entity.ClassId = request.ClassId ?? entity.ClassId;
        entity.Status = request.Status ?? entity.Status;
        entity.Active = request.Active ?? entity.Active;
        entity.InvoiceLink = request.InvoiceLink ?? entity.InvoiceLink;
        entity.RegistrationDate = request.RegistrationDate ?? entity.RegistrationDate;
        entity.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult(entity);
    }
}
