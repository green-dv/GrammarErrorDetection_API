using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Roles;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories
{
    public sealed class RoleRepository : IRoleRepository
    {
        private readonly IStoredProcedureExecutor executor;

        public RoleRepository(IStoredProcedureExecutor executor)
        {
            ArgumentNullException.ThrowIfNull(executor);
            this.executor = executor;
        }

        public async Task<List<RoleEntity>> List(PaginationDto<RoleList> request)
        {
            RepositoryValidation.ValidatePagination(request);
            var filter = request.Filter ?? new RoleList();

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "Role_List",
                [
                    new("Id", NpgsqlDbType.Uuid, filter.Id),
                    new("Name", NpgsqlDbType.Varchar, filter.Name),
                    new("Active", NpgsqlDbType.Boolean, filter.Active),
                    new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
                    new("PageSize", NpgsqlDbType.Integer, request.PageSize),
                    new("Results", NpgsqlDbType.Refcursor, "repository_list", ParameterDirection.InputOutput)
                ],
                StoredProcedureResult.Cursor("Results"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            return table.AsEnumerable().Select(ConvertToEntity.RoleToEntity).ToList();
        }

        public async Task<RoleEntity> Insert(RoleInsert request)
        {
            RepositoryValidation.ValidateInput(request);

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "Role_Insert",
                [
                    new("Name", NpgsqlDbType.Varchar, request.Name),
                    new("Active", NpgsqlDbType.Boolean, request.Active),
                    new("Role", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
                ],
                StoredProcedureResult.Composite("Role", "grammar", "Role"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            if (table.Rows.Count != 1)
                throw new DataException("Se esperaba exactamente una entidad.");

            return ConvertToEntity.RoleToEntity(table.Rows[0]);
        }

        public async Task<RoleEntity> Update(RoleUpdate request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "Role_Update",
                [
                    new("Id", NpgsqlDbType.Uuid, request.Id),
                    new("Name", NpgsqlDbType.Varchar, request.Name),
                    new("Active", NpgsqlDbType.Boolean, request.Active),
                    new("Role", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
                ],
                StoredProcedureResult.Composite("Role", "grammar", "Role"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            if (table.Rows.Count != 1)
                throw new DataException("Se esperaba exactamente una entidad.");

            return ConvertToEntity.RoleToEntity(table.Rows[0]);
        }
    }
}


