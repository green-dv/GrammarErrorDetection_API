using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.VerificationCodes;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories
{
    public sealed class VerificationCodeRepository : IVerificationCodeRepository
    {
        private readonly IStoredProcedureExecutor executor;

        public VerificationCodeRepository(IStoredProcedureExecutor executor)
        {
            ArgumentNullException.ThrowIfNull(executor);
            this.executor = executor;
        }

        public async Task<List<VerificationCodeEntity>> List(PaginationDto<VerificationCodeList> request)
        {
            RepositoryValidation.ValidatePagination(request);
            var filter = request.Filter ?? new VerificationCodeList();

            var response = await executor.ExecuteStoredProcedureAsync("grammar","VerificationCode_List",
                [
                    new("Id", NpgsqlDbType.Uuid, filter.Id),
                    new("CodeHash", NpgsqlDbType.Varchar, filter.CodeHash),
                    new("Purpose", NpgsqlDbType.Varchar, filter.Purpose),
                    new("AttemptCount", NpgsqlDbType.Integer, filter.AttemptCount),
                    new("ConsumedAt", NpgsqlDbType.Date, filter.ConsumedAt),
                    new("ExpirationTime", NpgsqlDbType.Date, filter.ExpirationTime),
                    new("UserId", NpgsqlDbType.Uuid, filter.UserId),
                    new("Email", NpgsqlDbType.Varchar, filter.Email),
                    new("Active", NpgsqlDbType.Boolean, filter.Active),
                    new("CreatedAt", NpgsqlDbType.Date, filter.CreatedAt),
                    new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
                    new("PageSize", NpgsqlDbType.Integer, request.PageSize),
                    new("Results", NpgsqlDbType.Refcursor, "repository_list", ParameterDirection.InputOutput)
                ],
                StoredProcedureResult.Cursor("Results"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            return table.AsEnumerable().Select(ConvertToEntity.VerificationCodeToEntity).ToList();
        }

        public async Task<VerificationCodeEntity> Insert(VerificationCodeInsert request)
        {
            RepositoryValidation.ValidateInput(request);

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "VerificationCode_Insert",
                [
                    new("CodeHash", NpgsqlDbType.Varchar, request.CodeHash),
                    new("Purpose", NpgsqlDbType.Varchar, request.Purpose),
                    new("ExpirationTime", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.ExpirationTime)),
                    new("AttemptCount", NpgsqlDbType.Integer, request.AttemptCount),
                    new("ConsumedAt", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.ConsumedAt)),
                    new("UserId", NpgsqlDbType.Uuid, request.UserId),
                    new("Email", NpgsqlDbType.Varchar, request.Email),
                    new("Active", NpgsqlDbType.Boolean, request.Active),
                    new("VerificationCode", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
                ],
                StoredProcedureResult.Composite("VerificationCode", "grammar", "VerificationCode"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            if (table.Rows.Count != 1)
                throw new DataException("Se esperaba exactamente una entidad.");

            return ConvertToEntity.VerificationCodeToEntity(table.Rows[0]);
        }

        public async Task<VerificationCodeEntity> Update(VerificationCodeUpdate request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "VerificationCode_Update",
                [
                    new("Id", NpgsqlDbType.Uuid, request.Id),
                    new("CodeHash", NpgsqlDbType.Varchar, request.CodeHash),
                    new("Purpose", NpgsqlDbType.Varchar, request.Purpose),
                    new("ExpirationTime", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.ExpirationTime)),
                    new("AttemptCount", NpgsqlDbType.Integer, request.AttemptCount),
                    new("ConsumedAt", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.ConsumedAt)),
                    new("UserId", NpgsqlDbType.Uuid, request.UserId),
                    new("Email", NpgsqlDbType.Varchar, request.Email),
                    new("Active", NpgsqlDbType.Boolean, request.Active),
                    new("VerificationCode", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
                ],
                StoredProcedureResult.Composite("VerificationCode", "grammar", "VerificationCode"));

            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            if (table.Rows.Count != 1)
                throw new DataException("Se esperaba exactamente una entidad.");

            return ConvertToEntity.VerificationCodeToEntity(table.Rows[0]);
        }
    }
}


