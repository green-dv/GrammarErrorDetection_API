using System.Data;
using GrammarErrorDetection.Models.Grammar.Users;

namespace GrammarErrorDetection.Repositories.Utils;

/// <summary>Mapeos de resultados de consultas a DTO, separados de las entidades de tabla.</summary>
public static class ConvertToDto
{
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

    /// <summary>Mapeo especial de la fila plana de User_List_Summary a sus DTO anidados.</summary>
    public static UserSummaryDto UserSummaryToDto(DataRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        var current = CourseSummaryToDto(row, "Current");
        var last = CourseSummaryToDto(row, "Last");
        return new UserSummaryDto
        {
            Id = row.Field<Guid>("Id"),
            FirstName = row.Field<string>("FirstName") ?? string.Empty,
            LastName = row.Field<string>("LastName") ?? string.Empty,
            Email = row.Field<string>("Email") ?? string.Empty,
            CI = row.Field<string>("CI") ?? string.Empty,
            Phone = row.Field<string?>("Phone"),
            IsEnrolled = row.Field<bool>("IsEnrolled"),
            CurrentCourse = current,
            LastCourse = last,
            LastCourseFinalScore = last is null ? null : row.Field<decimal?>("LastCourseFinalScore")
        };
    }
}
