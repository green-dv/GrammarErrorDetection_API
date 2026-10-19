using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories
{
    public sealed class RegistrationRepository : IRegistrationRepository
    {
        private readonly IStoredProcedureExecutor executor;

        public RegistrationRepository(IStoredProcedureExecutor executor)
        {
            ArgumentNullException.ThrowIfNull(executor);
            this.executor = executor;
        }

        public async Task<List<RegistrationEntity>> List(PaginationDto<RegistrationList> request)
        {
            RepositoryValidation.ValidatePagination(request);
            var filter = request.Filter ?? new RegistrationList();

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "Registration_List",
                [
                    new("Id", NpgsqlDbType.Uuid, filter.Id),
                    new("UserId", NpgsqlDbType.Uuid, filter.UserId),
                    new("ClassId", NpgsqlDbType.Uuid, filter.ClassId),
                    new("RegistrationDate", NpgsqlDbType.Date, filter.RegistrationDate),
                    new("Status", NpgsqlDbType.Varchar, filter.Status),
                    new("InvoiceLink", NpgsqlDbType.Varchar, filter.InvoiceLink),
                    new("CreatedAt", NpgsqlDbType.Date, filter.CreatedAt),
                    new("Active", NpgsqlDbType.Boolean, filter.Active),
                    new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
                    new("PageSize", NpgsqlDbType.Integer, request.PageSize),
                    new("Results", NpgsqlDbType.Refcursor, "repository_list", ParameterDirection.InputOutput)
                ],
                StoredProcedureResult.Cursor("Results"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            return table.AsEnumerable().Select(ConvertToEntity.RegistrationToEntity).ToList();
        }

        public async Task<RegistrationEntity> Insert(RegistrationInsert request)
        {
            RepositoryValidation.ValidateInput(request);

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "Registration_Insert",
                [
                    new("UserId", NpgsqlDbType.Uuid, request.UserId),
                    new("ClassId", NpgsqlDbType.Uuid, request.ClassId),
                    new("RegistrationDate", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.RegistrationDate)),
                    new("Status", NpgsqlDbType.Varchar, request.Status),
                    new("InvoiceLink", NpgsqlDbType.Varchar, request.InvoiceLink),
                    new("Active", NpgsqlDbType.Boolean, request.Active),
                    new("Registration", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
                ],
                StoredProcedureResult.Composite("Registration", "grammar", "Registration"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            if (table.Rows.Count != 1)
                throw new DataException("Se esperaba exactamente una entidad.");

            return ConvertToEntity.RegistrationToEntity(table.Rows[0]);
        }

        public async Task<RegistrationEntity> Update(RegistrationUpdate request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "Registration_Update",
                [
                    new("Id", NpgsqlDbType.Uuid, request.Id),
                    new("UserId", NpgsqlDbType.Uuid, request.UserId),
                    new("ClassId", NpgsqlDbType.Uuid, request.ClassId),
                    new("RegistrationDate", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.RegistrationDate)),
                    new("Status", NpgsqlDbType.Varchar, request.Status),
                    new("InvoiceLink", NpgsqlDbType.Varchar, request.InvoiceLink),
                    new("Active", NpgsqlDbType.Boolean, request.Active),
                    new("Registration", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
                ],
                StoredProcedureResult.Composite("Registration", "grammar", "Registration"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            if (table.Rows.Count != 1)
                throw new DataException("Se esperaba exactamente una entidad.");

            return ConvertToEntity.RegistrationToEntity(table.Rows[0]);
        }
    }
}

