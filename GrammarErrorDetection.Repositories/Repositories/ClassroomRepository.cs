using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Classrooms;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories
{
    public sealed class ClassroomRepository : IClassroomRepository
    {
        private readonly IStoredProcedureExecutor executor;

        public ClassroomRepository(IStoredProcedureExecutor executor)
        {
            ArgumentNullException.ThrowIfNull(executor);
            this.executor = executor;
        }

        public async Task<List<ClassroomEntity>> List(PaginationDto<ClassroomList> request)
        {
            RepositoryValidation.ValidatePagination(request);
            var filter = request.Filter ?? new ClassroomList();

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "Classroom_List",
                [
                    new("Id", NpgsqlDbType.Uuid, filter.Id),
                    new("Name", NpgsqlDbType.Varchar, filter.Name),
                    new("Capacity", NpgsqlDbType.Integer, filter.Capacity),
                    new("CreatedAt", NpgsqlDbType.Date, filter.CreatedAt),
                    new("Active", NpgsqlDbType.Boolean, filter.Active),
                    new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
                    new("PageSize", NpgsqlDbType.Integer, request.PageSize),
                    new("Results", NpgsqlDbType.Refcursor, "repository_list", ParameterDirection.InputOutput)
                ],
                StoredProcedureResult.Cursor("Results"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            return table.AsEnumerable().Select(ConvertToEntity.ClassroomToEntity).ToList();
        }

        public async Task<ClassroomEntity> Insert(ClassroomInsert request)
        {
            RepositoryValidation.ValidateInput(request);

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "Classroom_Insert",
                [
                    new("Name", NpgsqlDbType.Varchar, request.Name),
                    new("Capacity", NpgsqlDbType.Integer, request.Capacity),
                    new("Active", NpgsqlDbType.Boolean, request.Active),
                    new("Classroom", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
                ],
                StoredProcedureResult.Composite("Classroom", "grammar", "Classroom"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            if (table.Rows.Count != 1)
                throw new DataException("Se esperaba exactamente una entidad.");

            return ConvertToEntity.ClassroomToEntity(table.Rows[0]);
        }

        public async Task<ClassroomEntity> Update(ClassroomUpdate request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "Classroom_Update",
                [
                    new("Id", NpgsqlDbType.Uuid, request.Id),
                    new("Name", NpgsqlDbType.Varchar, request.Name),
                    new("Capacity", NpgsqlDbType.Integer, request.Capacity),
                    new("Active", NpgsqlDbType.Boolean, request.Active),
                    new("Classroom", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
                ],
                StoredProcedureResult.Composite("Classroom", "grammar", "Classroom"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            if (table.Rows.Count != 1)
                throw new DataException("Se esperaba exactamente una entidad.");

            return ConvertToEntity.ClassroomToEntity(table.Rows[0]);
        }
    }
}

