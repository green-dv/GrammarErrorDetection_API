using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classes;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories
{
    public sealed class ClassRepository : IClassRepository
    {
        private readonly IStoredProcedureExecutor executor;

        public ClassRepository(IStoredProcedureExecutor executor)
        {
            ArgumentNullException.ThrowIfNull(executor);
            this.executor = executor;
        }

        public async Task<List<ClassEntity>> List(PaginationDto<ClassList> request)
        {
            RepositoryValidation.ValidatePagination(request);
            var filter = request.Filter ?? new ClassList();

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "Class_List",
                [
                    new("Id", NpgsqlDbType.Uuid, filter.Id),
                    new("TeacherId", NpgsqlDbType.Uuid, filter.TeacherId),
                    new("ModuleNameId", NpgsqlDbType.Uuid, filter.ModuleNameId),
                    new("Bimester", NpgsqlDbType.Integer, filter.Bimester),
                    new("Year", NpgsqlDbType.Integer, filter.Year),
                    new("StartDate", NpgsqlDbType.Date, filter.StartDate),
                    new("EndDate", NpgsqlDbType.Date, filter.EndDate),
                    new("Parallel", NpgsqlDbType.Varchar, filter.Parallel),
                    new("ClassroomId", NpgsqlDbType.Uuid, filter.ClassroomId),
                    new("StartTime", NpgsqlDbType.Time, filter.StartTime),
                    new("EndTime", NpgsqlDbType.Time, filter.EndTime),
                    new("CreatedAt", NpgsqlDbType.Date, filter.CreatedAt),
                    new("Active", NpgsqlDbType.Boolean, filter.Active),
                    new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
                    new("PageSize", NpgsqlDbType.Integer, request.PageSize),
                    new("Results", NpgsqlDbType.Refcursor, "repository_list", ParameterDirection.InputOutput)
                ],
                StoredProcedureResult.Cursor("Results"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            return table.AsEnumerable().Select(ConvertToEntity.ClassListToEntity).ToList();
        }

        public async Task<ClassEntity> Insert(ClassInsert request)
        {
            RepositoryValidation.ValidateInput(request);

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "Class_Insert",
                [
                    new("TeacherId", NpgsqlDbType.Uuid, request.TeacherId),
                    new("ModuleNameId", NpgsqlDbType.Uuid, request.ModuleNameId),
                    new("Bimester", NpgsqlDbType.Integer, request.Bimester),
                    new("Year", NpgsqlDbType.Integer, request.Year),
                    new("StartDate", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.StartDate)),
                    new("EndDate", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.EndDate)),
                    new("Parallel", NpgsqlDbType.Varchar, request.Parallel),
                    new("ClassroomId", NpgsqlDbType.Uuid, request.ClassroomId),
                    new("StartTime", NpgsqlDbType.Time, request.StartTime),
                    new("EndTime", NpgsqlDbType.Time, request.EndTime),
                    new("Active", NpgsqlDbType.Boolean, request.Active),
                    new("Class", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
                ],
                StoredProcedureResult.Composite("Class", "grammar", "Class"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            if (table.Rows.Count != 1)
                throw new DataException("Se esperaba exactamente una entidad.");

            return ConvertToEntity.ClassToEntity(table.Rows[0]);
        }

        public async Task<ClassEntity> Update(ClassUpdate request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "Class_Update",
                [
                    new("Id", NpgsqlDbType.Uuid, request.Id),
                    new("TeacherId", NpgsqlDbType.Uuid, request.TeacherId),
                    new("ModuleNameId", NpgsqlDbType.Uuid, request.ModuleNameId),
                    new("Bimester", NpgsqlDbType.Integer, request.Bimester),
                    new("Year", NpgsqlDbType.Integer, request.Year),
                    new("StartDate", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.StartDate)),
                    new("EndDate", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.EndDate)),
                    new("Parallel", NpgsqlDbType.Varchar, request.Parallel),
                    new("ClassroomId", NpgsqlDbType.Uuid, request.ClassroomId),
                    new("StartTime", NpgsqlDbType.Time, request.StartTime),
                    new("EndTime", NpgsqlDbType.Time, request.EndTime),
                    new("Active", NpgsqlDbType.Boolean, request.Active),
                    new("Class", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
                ],
                StoredProcedureResult.Composite("Class", "grammar", "Class"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            if (table.Rows.Count != 1)
                throw new DataException("Se esperaba exactamente una entidad.");

            return ConvertToEntity.ClassToEntity(table.Rows[0]);
        }
    }
}

