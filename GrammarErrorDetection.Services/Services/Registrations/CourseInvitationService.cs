using System.Buffers.Binary;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Models.Grammar.Classrooms;
using GrammarErrorDetection.Models.Grammar.Evaluations;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Configuration;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

/// <summary>
/// Invitaciones a un curso por enlace/QR. El token es stateless: ClassId + expiración firmados con HMAC-SHA256
/// (clave derivada de JWT_SECRET), por lo que no requiere tablas. Unirse equivale a una aprobación del docente o
/// coordinador: no se exige haber completado el curso anterior, pero sí cupo y no tener otro curso sin examen 2.
/// </summary>
public sealed class CourseInvitationService : ICourseInvitationService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);
    private const int PayloadLength = 16 + 8;
    private const int SignatureLength = 32;

    private readonly IRegistrationRepository registrations;
    private readonly IClassRepository classes;
    private readonly IClassroomRepository classrooms;
    private readonly IEvaluationRepository evaluations;
    private readonly TimeProvider clock;
    private readonly byte[] key;

    public CourseInvitationService(IRegistrationRepository registrations, IClassRepository classes,
        IClassroomRepository classrooms, IEvaluationRepository evaluations, JwtOptions jwt, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(classes);
        ArgumentNullException.ThrowIfNull(classrooms);
        ArgumentNullException.ThrowIfNull(evaluations);
        ArgumentNullException.ThrowIfNull(jwt);
        ArgumentNullException.ThrowIfNull(clock);
        this.registrations = registrations;
        this.classes = classes;
        this.classrooms = classrooms;
        this.evaluations = evaluations;
        this.clock = clock;
        // Clave propia del propósito: un JWT jamás puede pasar por token de invitación.
        key = SHA256.HashData(Encoding.UTF8.GetBytes("course-invitation:" + jwt.Secret));
    }

    public async Task<CourseInvitationLinkDto> CreateLink(Guid userId, bool isCoordinator, Guid classId)
    {
        if (classId == Guid.Empty) throw new ValidationException("El identificador del curso no puede estar vacío.");
        var course = await FindClass(classId);
        if (!isCoordinator && course.TeacherId != userId)
            throw new AccessDeniedException("Solo el docente del curso o un coordinador puede generar su enlace de invitación.");
        EnsureOpen(course);
        var expires = clock.GetUtcNow().UtcDateTime.Add(Lifetime);
        return new CourseInvitationLinkDto { ClassId = classId, Token = Sign(classId, expires), ExpiresAt = expires };
    }

    public async Task<CourseJoinResultDto> Describe(string token) =>
        Result(null, await FindClass(Verify(token)), alreadyMember: false);

    public async Task<CourseJoinResultDto> Preview(Guid studentId, string token)
    {
        var (course, inTarget, alreadyMember) = await Check(studentId, token);
        return Result(inTarget, course, alreadyMember);
    }

    public async Task<CourseJoinResultDto> Join(Guid studentId, string token)
    {
        var (course, inTarget, alreadyMember) = await Check(studentId, token);
        if (alreadyMember) return Result(inTarget, course, alreadyMember: true);
        var classId = course.Id;
        var now = DateTime.UtcNow;
        RegistrationEntity saved;
        if (inTarget is not null)
            saved = await registrations.Update(new RegistrationUpdate
            { Id = inTarget.Id, ClassId = classId, Status = "Approved", Active = true, RegistrationDate = now });
        else
            saved = await registrations.Insert(new RegistrationInsert
            { UserId = studentId, ClassId = classId, Status = "Approved", Active = true, RegistrationDate = now });
        return Result(saved, course, alreadyMember: false);
    }

    /// <summary>Todas las validaciones de la invitación, sin escribir nada (las comparten la vista previa y Join).</summary>
    private async Task<(ClassEntity Course, RegistrationEntity? InTarget, bool AlreadyMember)> Check(Guid studentId, string token)
    {
        if (studentId == Guid.Empty) throw new ValidationException("El identificador de estudiante no puede estar vacío.");
        var classId = Verify(token);
        var course = await FindClass(classId);
        EnsureOpen(course);

        var own = await ReadAll(registrations.List, new RegistrationList { UserId = studentId, Active = true });
        var mine = own.Where(r => r.UserId == studentId && r.ClassId.HasValue
            && ManualClassEligibility.ReservesSeat(r)).ToList();
        var inTarget = mine.Where(r => r.ClassId == classId).OrderByDescending(r => r.UpdatedAt).FirstOrDefault();
        if (inTarget is not null && string.Equals(inTarget.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            return (course, inTarget, true);

        // Curso pendiente = inscripción vigente en otro curso cuyo examen 2 aún no tiene nota completa.
        foreach (var other in mine.Where(r => r.ClassId != classId))
        {
            var grades = await ReadAll(evaluations.List,
                new EvaluationList { RegistrationId = other.Id, Active = true });
            if (RegistrationEvaluationCalculator.CalculateExam(grades.Where(e => e.RegistrationId == other.Id), 2) is null)
                throw new AuthConflictException("Ya perteneces a un curso que aún no termina (sin la nota del examen 2). No puedes estar en 2 cursos a la vez.");
        }

        await EnsureSeat(course, studentId);
        return (course, inTarget, false);
    }

    private async Task EnsureSeat(ClassEntity course, Guid studentId)
    {
        var room = (await ReadAll(classrooms.List, new ClassroomList { Id = course.ClassroomId, Active = true }))
            .FirstOrDefault(r => r.Id == course.ClassroomId)
            ?? throw new ValidationException("El aula del curso no está disponible.");
        var occupied = (await ReadAll(registrations.List, new RegistrationList { ClassId = course.Id, Active = true }))
            .Count(r => r.ClassId == course.Id && r.UserId != studentId && ManualClassEligibility.ReservesSeat(r));
        if (occupied >= room.Capacity) throw new ValidationException("El curso ya no tiene cupos disponibles.");
    }

    private async Task<ClassEntity> FindClass(Guid classId) =>
        (await ReadAll(classes.List, new ClassList { Id = classId })).FirstOrDefault(c => c.Id == classId)
        ?? throw new KeyNotFoundException("El curso no existe.");

    private void EnsureOpen(ClassEntity course)
    {
        var today = clock.GetUtcNow().UtcDateTime.AddHours(-4).Date;
        if (!course.Active || (course.EndDate is not null && course.EndDate.Value.Date < today))
            throw new ValidationException("El curso ya no está disponible para nuevas inscripciones.");
    }

    private static CourseJoinResultDto Result(RegistrationEntity? registration, ClassEntity course, bool alreadyMember) => new()
    {
        RegistrationId = registration?.Id ?? Guid.Empty, ClassId = course.Id, ModuleName = course.ModuleName ?? string.Empty,
        Parallel = course.Parallel, Year = course.Year, Bimester = course.Bimester,
        StartTime = course.StartTime, EndTime = course.EndTime, TeacherName = course.TeacherName,
        ClassroomName = course.ClassroomName, AlreadyMember = alreadyMember
    };

    private string Sign(Guid classId, DateTime expiresUtc)
    {
        Span<byte> payload = stackalloc byte[PayloadLength];
        classId.TryWriteBytes(payload);
        BinaryPrimitives.WriteInt64BigEndian(payload[16..], new DateTimeOffset(expiresUtc, TimeSpan.Zero).ToUnixTimeSeconds());
        var signature = HMACSHA256.HashData(key, payload);
        var bytes = new byte[PayloadLength + SignatureLength];
        payload.CopyTo(bytes);
        signature.CopyTo(bytes, PayloadLength);
        return Base64Url(bytes);
    }

    private Guid Verify(string? token)
    {
        const string invalid = "El enlace de invitación no es válido.";
        byte[] bytes;
        try { bytes = FromBase64Url(token?.Trim() ?? ""); }
        catch (FormatException) { throw new ValidationException(invalid); }
        if (bytes.Length != PayloadLength + SignatureLength) throw new ValidationException(invalid);
        var expected = HMACSHA256.HashData(key, bytes.AsSpan(0, PayloadLength));
        if (!CryptographicOperations.FixedTimeEquals(expected, bytes.AsSpan(PayloadLength))) throw new ValidationException(invalid);
        var expires = DateTimeOffset.FromUnixTimeSeconds(BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(16, 8)));
        if (expires <= clock.GetUtcNow()) throw new ValidationException("El enlace de invitación expiró. Pide uno nuevo a tu docente.");
        return new Guid(bytes.AsSpan(0, 16));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        var text = value.Replace('-', '+').Replace('_', '/');
        text = text.PadRight(text.Length + (4 - text.Length % 4) % 4, '=');
        return Convert.FromBase64String(text);
    }

    private static async Task<List<T>> ReadAll<TFilter, T>(Func<PaginationDto<TFilter>, Task<List<T>>> list, TFilter filter)
    {
        var result = new List<T>();
        const int pageSize = 200;
        for (var page = 1; ; page++)
        {
            var batch = await list(new PaginationDto<TFilter> { PageNumber = page, PageSize = pageSize, Filter = filter });
            result.AddRange(batch);
            if (batch.Count < pageSize) return result;
        }
    }
}
