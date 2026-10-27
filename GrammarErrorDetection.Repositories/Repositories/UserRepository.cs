using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly IStoredProcedureExecutor executor;

        public UserRepository(IStoredProcedureExecutor executor)
        {
            ArgumentNullException.ThrowIfNull(executor);
            this.executor = executor;
        }

        public async Task<List<UserEntity>> List(PaginationDto<UserList> request)
        {
            RepositoryValidation.ValidatePagination(request);
            var filter = request.Filter ?? new UserList();

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "User_List",
                [
                    new("FirstName", NpgsqlDbType.Varchar, filter.FirstName),
                    new("LastName", NpgsqlDbType.Varchar, filter.LastName),
                    new("CI", NpgsqlDbType.Varchar, filter.CI),
                    new("Phone", NpgsqlDbType.Varchar, filter.Phone),
                    new("Email", NpgsqlDbType.Varchar, filter.Email),
                    new("CreatedAt", NpgsqlDbType.Date, filter.CreatedAt),
                    new("RoleId", NpgsqlDbType.Uuid, filter.RoleId),
                    new("Active", NpgsqlDbType.Boolean, filter.Active),
                    new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
                    new("PageSize", NpgsqlDbType.Integer, request.PageSize),
                    new("Users", NpgsqlDbType.Refcursor, "repository_list", ParameterDirection.InputOutput),
                    new("CurrentModuleNameId", NpgsqlDbType.Uuid, filter.CurrentModuleNameId),
                    new("ClassId", NpgsqlDbType.Uuid, filter.ClassId)
                ],
                StoredProcedureResult.Cursor("Users"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            return table.AsEnumerable().Select(ConvertToDto.UserListToEntity).ToList();
        }

        public async Task<UserEntity> Insert(UserInsert request)
        {
            RepositoryValidation.ValidateInput(request);

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "User_Insert",
                [
                    new("FechaNacimiento", NpgsqlDbType.Date, request.FechaNacimiento),
                    new("FirstName", NpgsqlDbType.Varchar, request.FirstName),
                    new("LastName", NpgsqlDbType.Varchar, request.LastName),
                    new("CI", NpgsqlDbType.Varchar, request.CI),
                    new("Email", NpgsqlDbType.Varchar, request.Email),
                    new("Password", NpgsqlDbType.Varchar, request.Password),
                    new("RoleId", NpgsqlDbType.Uuid, request.RoleId),
                    new("Phone", NpgsqlDbType.Varchar, request.Phone),
                    new("HasAuthenticator", NpgsqlDbType.Boolean, request.HasAuthenticator),
                    new("TotpSecret", NpgsqlDbType.Varchar, request.TotpSecret),
                    new("IsTotpEnabled", NpgsqlDbType.Boolean, request.IsTotpEnabled),
                    new("ChangePassword", NpgsqlDbType.Boolean, request.ChangePassword),
                    new("Active", NpgsqlDbType.Boolean, request.Active),
                    new("User", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
                ],
                StoredProcedureResult.Composite("User", "grammar", "User"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            if (table.Rows.Count != 1)
                throw new DataException("Se esperaba exactamente una entidad.");

            return ConvertToEntity.UserToEntity(table.Rows[0]);
        }

        public async Task<UserEntity> Update(UserUpdate request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "User_Update",
                [
                    new("FechaNacimiento", NpgsqlDbType.Date, request.FechaNacimiento),
                    new("Id", NpgsqlDbType.Uuid, request.Id),
                    new("FirstName", NpgsqlDbType.Varchar, request.FirstName),
                    new("LastName", NpgsqlDbType.Varchar, request.LastName),
                    new("CI", NpgsqlDbType.Varchar, request.CI),
                    new("Email", NpgsqlDbType.Varchar, request.Email),
                    new("Password", NpgsqlDbType.Varchar, request.Password),
                    new("RoleId", NpgsqlDbType.Uuid, request.RoleId),
                    new("Phone", NpgsqlDbType.Varchar, request.Phone),
                    new("HasAuthenticator", NpgsqlDbType.Boolean, request.HasAuthenticator),
                    new("TotpSecret", NpgsqlDbType.Varchar, request.TotpSecret),
                    new("IsTotpEnabled", NpgsqlDbType.Boolean, request.IsTotpEnabled),
                    new("ChangePassword", NpgsqlDbType.Boolean, request.ChangePassword),
                    new("Active", NpgsqlDbType.Boolean, request.Active),
                    new("User", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
                ],
                StoredProcedureResult.Composite("User", "grammar", "User"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            if (table.Rows.Count != 1)
                throw new DataException("Se esperaba exactamente una entidad.");

            return ConvertToEntity.UserToEntity(table.Rows[0]);
        }
    }
}


