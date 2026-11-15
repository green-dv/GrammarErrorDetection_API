using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.Grammar.Reports;
using GrammarErrorDetection.Repositories.Interfaces;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class StudentProgressRepository(IStoredProcedureExecutor executor) : IStudentProgressRepository
{
    public async Task<List<StudentProgressDto>> List(Guid? classId, Guid? userId)
    {
        var result = await executor.ExecuteStoredProcedureAsync("grammar", "Class_GradeProgress_List",
        [
            new("ClassId", NpgsqlDbType.Uuid, classId),
            new("UserId", NpgsqlDbType.Uuid, userId),
            new("Results", NpgsqlDbType.Refcursor, "class_grade_progress_list", ParameterDirection.InputOutput)
        ], StoredProcedureResult.Cursor("Results"));

        using var table = result.GetDataOrThrow() ?? throw new DataException("El procedimiento no devolvió una tabla.");
        return table.AsEnumerable().Select(row => new StudentProgressDto(
            row.Field<Guid>("UserId"),
            row.Field<string>("StudentName"),
            row.Field<Guid>("ClassId"),
            row.Field<Guid>("RegistrationId"),
            row.Field<Guid>("ModuleNameId"),
            row.Field<string>("ModuleName") ?? throw new DataException("El módulo no tiene nombre."),
            row.Field<int>("Level"),
            row.Field<int>("Year"),
            row.Field<int>("Bimester"),
            row.Field<string>("Parallel"),
            row.Field<decimal?>("Exam1Score"),
            row.Field<decimal?>("Exam2Score"),
            row.Field<decimal?>("FinalScore"))).ToList();
    }
}
