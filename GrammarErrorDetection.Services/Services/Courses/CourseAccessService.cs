using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class CourseAccessService : ICourseAccessService
{
    private const string Denied = "Solo puedes gestionar los cursos que tienes a tu cargo.";
    private readonly IClassRepository classes;
    private readonly IRegistrationRepository registrations;

    public CourseAccessService(IClassRepository classes, IRegistrationRepository registrations)
    {
        ArgumentNullException.ThrowIfNull(classes);
        ArgumentNullException.ThrowIfNull(registrations);
        this.classes = classes;
        this.registrations = registrations;
    }

    public async Task EnsureOwnsClass(Guid teacherId, Guid classId)
    {
        if (classId == Guid.Empty) throw new ValidationException("El identificador del curso no puede estar vacío.");
        var found = await classes.List(new PaginationDto<ClassList>
        { PageNumber = 1, PageSize = 1, Filter = new ClassList { Id = classId } });
        var course = found.FirstOrDefault(c => c.Id == classId) ?? throw new KeyNotFoundException("El curso no existe.");
        if (course.TeacherId != teacherId) throw new AccessDeniedException(Denied);
    }

    public async Task EnsureOwnsRegistration(Guid teacherId, Guid registrationId)
    {
        if (registrationId == Guid.Empty) throw new ValidationException("El identificador de inscripción no puede estar vacío.");
        var found = await registrations.List(new PaginationDto<RegistrationList>
        { PageNumber = 1, PageSize = 1, Filter = new RegistrationList { Id = registrationId } });
        var registration = found.FirstOrDefault(r => r.Id == registrationId) ?? throw new KeyNotFoundException("La inscripción no existe.");
        if (registration.ClassId is not Guid classId) throw new AccessDeniedException(Denied);
        await EnsureOwnsClass(teacherId, classId);
    }

    public async Task<HashSet<Guid>> OwnedClassIds(Guid teacherId)
    {
        var owned = new HashSet<Guid>();
        const int pageSize = 200;
        for (var page = 1; ; page++)
        {
            var batch = await classes.List(new PaginationDto<ClassList>
            { PageNumber = page, PageSize = pageSize, Filter = new ClassList { TeacherId = teacherId } });
            foreach (var course in batch.Where(c => c.TeacherId == teacherId)) owned.Add(course.Id);
            if (batch.Count < pageSize) return owned;
        }
    }
}
