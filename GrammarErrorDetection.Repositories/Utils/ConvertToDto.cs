using System.Data;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Models.Grammar.Texts;
using System.Text.Json;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;

namespace GrammarErrorDetection.Repositories.Utils;

/// <summary>Mapeos de resultados de consultas a DTO, separados de las entidades de tabla.</summary>
public static class ConvertToDto
{
    public static SupportMaterialListItemDto SupportMaterialListToDto(DataRow row)
    {
        var material = ConvertToEntity.SupportMaterialToEntity(row);
        return new SupportMaterialListItemDto
        {
            Id = material.Id, Name = material.Name, ErrorClassificationId = material.ErrorClassificationId,
            UploadDate = material.UploadDate, FileLink = material.FileLink, FileTypeId = material.FileTypeId,
            FileSize = material.FileSize, UploadedBy = material.UploadedBy, Active = material.Active,
            CreatedAt = material.CreatedAt, UpdatedAt = material.UpdatedAt,
            ErrorClassificationName = row.Field<string>("ErrorClassificationName")
                ?? throw new DataException("Falta ErrorClassificationName en el listado de material de apoyo."),
            UploadedByName = row.Field<string>("UploadedByName")
                ?? throw new DataException("Falta UploadedByName en el listado de material de apoyo.")
        };
    }

    public static TextListItemDto TextListToDto(DataRow row)
    {
        var text = ConvertToEntity.TextToEntity(row);
        return new TextListItemDto
        {
            Id = text.Id, UserId = text.UserId, SubmissionDate = text.SubmissionDate,
            OriginalContent = text.OriginalContent, Status = text.Status,
            CreatedAt = text.CreatedAt, UpdatedAt = text.UpdatedAt, Active = text.Active,
            Sentences = ReadTextSentences(row)
        };
    }

    // Text_Detail_List devuelve t.* y las oraciones con errores en Sentences (JSONB).
    private static List<SentenceErrorDto> ReadTextSentences(DataRow row)
    {
        if (!row.Table.Columns.Contains("Sentences"))
            throw new DataException("Text_Detail_List no devolvió la columna Sentences.");
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var sentences = row["Sentences"] switch
            {
                DBNull => [],
                string json => JsonSerializer.Deserialize<List<SentenceErrorDto>>(json, options) ?? [],
                JsonDocument document => document.RootElement.Deserialize<List<SentenceErrorDto>>(options) ?? [],
                JsonElement element => element.Deserialize<List<SentenceErrorDto>>(options) ?? [],
                _ => throw new DataException("Sentences debe contener las oraciones y sus errores; no una lista de IDs.")
            };
            return sentences.OrderBy(s => s.SentenceIndex).ToList();
        }
        catch (JsonException exception)
        {
            throw new DataException("El detalle de Sentences no corresponde al DTO de oraciones y errores.", exception);
        }
    }

    public static RegistrationListItemDto RegistrationListToDto(DataRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return new RegistrationListItemDto
        {
            RegistrationId = row.Field<Guid>("RegistrationId"),
            UserId = row.Field<Guid>("UserId"),
            ClassId = row.Field<Guid?>("ClassId"),
            FirstName = row.Field<string>("FirstName") ?? throw new DataException("Falta FirstName en el listado de inscripciones."),
            LastName = row.Field<string>("LastName") ?? throw new DataException("Falta LastName en el listado de inscripciones."),
            CI = row.Field<string>("CI") ?? throw new DataException("Falta CI en el listado de inscripciones."),
            Phone = row.Field<string?>("Phone"),
            ModuleName = row.Field<string?>("ModuleName"),
            ModuleId = row.Field<Guid?>("ModuleId"),
            RegistrationDate = ReadDate(row, "RegistrationDate"),
            Status = row.Field<string>("Status") ?? throw new DataException("Falta Status en el listado de inscripciones."),
            UpdatedAt = ReadDate(row, "UpdatedAt") ?? throw new DataException("Falta UpdatedAt en el listado de inscripciones.")
        };
    }

    /// <summary>Construye un curso opcional desde las columnas Current* o Last* del SP.</summary>
    public static CourseSummaryDto? CourseSummaryToDto(DataRow row, string prefix)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        var classId = row.Field<Guid?>($"{prefix}ClassId");
        if (classId is null) return null;

        return new CourseSummaryDto
        {
            ClassId = classId.Value,
            ModuleNameId = row.Field<Guid>($"{prefix}ModuleNameId"),
            ModuleName = row.Field<string>($"{prefix}ModuleName")
                ?? throw new DataException("El resumen de curso no contiene el nombre del módulo."),
            Year = row.Field<int>($"{prefix}Year"),
            Bimester = row.Field<int>($"{prefix}Bimester"),
            Parallel = row.Field<string?>($"{prefix}Parallel"),
            RegistrationStatus = row.Field<string>($"{prefix}RegistrationStatus")
                ?? throw new DataException("El resumen de curso no contiene el estado de inscripción.")
        };
    }

    /// <summary>Mapea User_List conservando credenciales para los consumidores internos.</summary>
    public static UserEntity UserListToEntity(DataRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        var user = ConvertToEntity.UserToEntity(row);
        user.RoleName = row.Field<string>("RoleName")
            ?? throw new DataException("El listado de usuarios no contiene el nombre del rol.");
        var current = CourseSummaryToDto(row, "Current");
        if (current is not null)
        {
            user.CurrentCourse = new CurrentCourseDto
            {
                ClassId = current.ClassId, ModuleNameId = current.ModuleNameId,
                ModuleName = current.ModuleName, Year = current.Year, Bimester = current.Bimester,
                Parallel = current.Parallel, RegistrationStatus = current.RegistrationStatus,
                TeacherId = row.Field<Guid>("CurrentTeacherId"),
                TeacherName = row.Field<string>("CurrentTeacherName")
                    ?? throw new DataException("El curso actual no contiene el nombre del docente."),
                ClassroomId = row.Field<Guid>("CurrentClassroomId"),
                ClassroomName = row.Field<string>("CurrentClassroomName")
                    ?? throw new DataException("El curso actual no contiene el nombre del aula."),
                ClassroomCapacity = row.Field<int>("CurrentClassroomCapacity"),
                StartDate = ReadDate(row, "CurrentStartDate"),
                EndDate = ReadDate(row, "CurrentEndDate"),
                StartTime = ReadTime(row, "CurrentStartTime"),
                EndTime = ReadTime(row, "CurrentEndTime"),
                RegistrationId = row.Field<Guid>("CurrentRegistrationId"),
                RegistrationDate = ReadDate(row, "CurrentRegistrationDate")
            };
        }
        user.CurrentCourseFinalScore = current is null ? null : row.Field<decimal?>("CurrentCourseFinalScore");
        user.LastCourse = CourseSummaryToDto(row, "Last");
        return user;
    }

    private static DateTime? ReadDate(DataRow row, string column) => row[column] switch
    {
        DBNull => null,
        DateTime value => value,
        DateOnly value => value.ToDateTime(TimeOnly.MinValue),
        _ => throw new DataException($"La columna {column} no contiene una fecha válida.")
    };

    private static TimeOnly? ReadTime(DataRow row, string column) => row[column] switch
    {
        DBNull => null,
        TimeOnly value => value,
        TimeSpan value => TimeOnly.FromTimeSpan(value),
        _ => throw new DataException($"La columna {column} no contiene una hora válida.")
    };
}
