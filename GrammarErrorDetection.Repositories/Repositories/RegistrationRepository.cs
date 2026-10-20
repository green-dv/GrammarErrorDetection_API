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

        public async Task<List<RegistrationListItemDto>> ListSummary(PaginationDto<RegistrationSummaryList> request)
        {
            RepositoryValidation.ValidatePagination(request);
            using var table = await ReadListPage(request);
            return table.AsEnumerable().Select(ConvertToDto.RegistrationListToDto).ToList();
        }

        private async Task<DataTable> ReadListPage(PaginationDto<RegistrationSummaryList> request)
        {
            var filter = request.Filter ?? new RegistrationSummaryList();

            var response = await executor.ExecuteStoredProcedureAsync(
                "grammar",
                "Registration_List",
                [
                    new("UserId", NpgsqlDbType.Uuid, filter.UserId),
                    new("ModuleNameId", NpgsqlDbType.Uuid, filter.ModuleNameId),
                    new("RegistrationDate", NpgsqlDbType.Date, filter.RegistrationDate),
                    new("Status", NpgsqlDbType.Varchar, filter.Status),
                    new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
                    new("PageSize", NpgsqlDbType.Integer, request.PageSize),
                    new("Results", NpgsqlDbType.Refcursor, "repository_list", ParameterDirection.InputOutput)
                ],
                StoredProcedureResult.Cursor("Results"));

            return response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        }

        // Se usan los mismos parámetros del SP público. Los filtros internos que
        // no admite su firma se aplican antes de paginar el resultado solicitado.
        public async Task<List<RegistrationEntity>> List(PaginationDto<RegistrationList> request)
        {
            RepositoryValidation.ValidatePagination(request);
            var filter = request.Filter ?? new RegistrationList();
            if (filter.Id is null && filter.ClassId is null && filter.Active is null
                && filter.InvoiceLink is null && filter.CreatedAt is null)
            {
                using var table = await ReadListPage(new PaginationDto<RegistrationSummaryList>
                {
                    PageNumber = request.PageNumber, PageSize = request.PageSize,
                    Filter = new RegistrationSummaryList
                    {
                        UserId = filter.UserId, RegistrationDate = filter.RegistrationDate, Status = filter.Status
                    }
                });
                return table.AsEnumerable().Select(ConvertToEntity.RegistrationListToEntity).ToList();
            }
            var result = new List<RegistrationEntity>();
            var offset = ((long)request.PageNumber - 1) * request.PageSize;
            long matched = 0;
            const int batchSize = 200;
            for (var page = 1; ; page++)
            {
                using var table = await ReadListPage(new PaginationDto<RegistrationSummaryList>
                {
                    PageNumber = page, PageSize = batchSize,
                    Filter = new RegistrationSummaryList
                    {
                        UserId = filter.UserId, RegistrationDate = filter.RegistrationDate, Status = filter.Status
                    }
                });
                foreach (var row in table.AsEnumerable())
                {
                    var registration = ConvertToEntity.RegistrationListToEntity(row);
                    if (filter.Active.HasValue && !registration.Active.HasValue)
                        throw new DataException("Registration_List no devuelve Active; no se pueden filtrar inscripciones activas sin ese dato.");
                    if (filter.CreatedAt.HasValue && !registration.CreatedAt.HasValue)
                        throw new DataException("Registration_List no devuelve CreatedAt; no se puede aplicar ese filtro.");
                    if (filter.InvoiceLink is not null && !table.Columns.Contains("InvoiceLink"))
                        throw new DataException("Registration_List no devuelve InvoiceLink; no se puede aplicar ese filtro.");
                    if ((filter.Id.HasValue && registration.Id != filter.Id.Value)
                        || (filter.ClassId.HasValue && registration.ClassId != filter.ClassId.Value)
                        || (filter.Active.HasValue && registration.Active != filter.Active.Value)
                        || (filter.InvoiceLink is not null && registration.InvoiceLink != filter.InvoiceLink)
                        || (filter.CreatedAt.HasValue && DateOnly.FromDateTime(registration.CreatedAt!.Value) != filter.CreatedAt.Value))
                        continue;
                    if (matched++ < offset) continue;
                    result.Add(registration);
                    if (result.Count == request.PageSize) return result;
                }
                if (table.Rows.Count < batchSize) return result;
            }
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

