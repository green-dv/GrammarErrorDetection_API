using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.ModuleNames;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories
{
    public sealed class ModuleNameRepository : IModuleNameRepository
    {
        private readonly IStoredProcedureExecutor executor;

        public ModuleNameRepository(IStoredProcedureExecutor executor)
        {
            ArgumentNullException.ThrowIfNull(executor);
            this.executor = executor;
        }

        public async Task<List<ModuleNameEntity>> List(PaginationDto<ModuleNameList> request)
        {
            RepositoryValidation.ValidatePagination(request);
            var filter = request.Filter ?? new ModuleNameList();

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "ModuleName_List",
                [
                    new("Id", NpgsqlDbType.Uuid, filter.Id),
                    new("Name", NpgsqlDbType.Varchar, filter.Name),
                    new("Level", NpgsqlDbType.Integer, filter.Level),
                    new("NextModuleId", NpgsqlDbType.Uuid, filter.NextModuleId),
                    new("Active", NpgsqlDbType.Boolean, filter.Active),
                    new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
                    new("PageSize", NpgsqlDbType.Integer, request.PageSize),
                    new("Results", NpgsqlDbType.Refcursor, "repository_list", ParameterDirection.InputOutput)
                ],
                StoredProcedureResult.Cursor("Results"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            return table.AsEnumerable().Select(ConvertToEntity.ModuleNameToEntity).ToList();
        }

        public async Task<ModuleNameEntity> Insert(ModuleNameInsert request)
        {
            RepositoryValidation.ValidateInput(request);

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "ModuleName_Insert",
                [
                    new("Name", NpgsqlDbType.Varchar, request.Name),
                    new("Level", NpgsqlDbType.Integer, request.Level),
                    new("NextModuleId", NpgsqlDbType.Uuid, request.NextModuleId),
                    new("Active", NpgsqlDbType.Boolean, request.Active),
                    new("ModuleName", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
                ],
                StoredProcedureResult.Composite("ModuleName", "grammar", "ModuleName"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            if (table.Rows.Count != 1)
                throw new DataException("Se esperaba exactamente una entidad.");

            return ConvertToEntity.ModuleNameToEntity(table.Rows[0]);
        }

        public async Task<ModuleNameEntity> Update(ModuleNameUpdate request)
        {
            RepositoryValidation.ValidateInput(request);

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "ModuleName_Update",
                [
                    new("Id", NpgsqlDbType.Uuid, request.Id),
                    new("Name", NpgsqlDbType.Varchar, request.Name),
                    new("Level", NpgsqlDbType.Integer, request.Level),
                    new("NextModuleId", NpgsqlDbType.Uuid, request.NextModuleId),
                    new("ClearNextModule", NpgsqlDbType.Boolean, request.ClearNextModule),
                    new("Active", NpgsqlDbType.Boolean, request.Active),
                    new("ModuleName", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
                ],
                StoredProcedureResult.Composite("ModuleName", "grammar", "ModuleName"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            if (table.Rows.Count != 1)
                throw new DataException("Se esperaba exactamente una entidad.");

            return ConvertToEntity.ModuleNameToEntity(table.Rows[0]);
        }
    }
}

