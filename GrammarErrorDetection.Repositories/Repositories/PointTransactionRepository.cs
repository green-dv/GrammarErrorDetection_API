using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.Grammar.PointTransactions;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class PointTransactionRepository(IStoredProcedureExecutor executor) : IPointTransactionRepository
{
    public async Task<List<PointTransactionEntity>> List(PointTransactionListQuery request)
    {
        RepositoryValidation.ValidatePagination(request);
        var filter = request.Filter ?? new PointTransactionList();
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "PointTransaction_List",
            [
                new("Id", NpgsqlDbType.Uuid, filter.Id),
                new("RegistrationId", NpgsqlDbType.Uuid, filter.RegistrationId),
                new("UserId", NpgsqlDbType.Uuid, filter.UserId),
                new("Amount", NpgsqlDbType.Integer, filter.Amount),
                new("Reason", NpgsqlDbType.Varchar, filter.Reason),
                new("GrantedAt", NpgsqlDbType.Date, filter.GrantedAt),
                new("Active", NpgsqlDbType.Boolean, filter.Active),
                new("CreatedAt", NpgsqlDbType.Date, filter.CreatedAt),
                new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
                new("PageSize", NpgsqlDbType.Integer, request.PageSize),
                new("Results", NpgsqlDbType.Refcursor, "pointtransaction_list", ParameterDirection.InputOutput)
            ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(ToEntity).ToList();
    }

    public async Task<PointTransactionTotalDto> Total(PointTransactionTotalQuery request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "PointTransaction_Total",
            [
                new("ClassId", NpgsqlDbType.Uuid, request.ClassId),
                new("UserId", NpgsqlDbType.Uuid, request.UserId),
                new("Results", NpgsqlDbType.Refcursor, "pointtransaction_total", ParameterDirection.InputOutput)
            ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente un total.");
        return new() { TotalPoints = table.Rows[0].Field<long>("TotalPoints") };
    }

    public async Task<PointTransactionEntity> Insert(PointTransactionInsert request)
    {
        RepositoryValidation.ValidateInput(request);
        List<StoredProcedureParameter> parameters =
        [
            new("RegistrationId", NpgsqlDbType.Uuid, request.RegistrationId),
            new("Amount", NpgsqlDbType.Integer, request.Amount),
            new("Reason", NpgsqlDbType.Varchar, request.Reason),
            new("PointTransaction", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
        ];
        // Omitir el argumento conserva el DEFAULT de fecha definido por el SP.
        if (request.GrantedAt is not null)
            parameters.Add(new("GrantedAt", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.GrantedAt)));
        parameters.Add(new("Active", NpgsqlDbType.Boolean, request.Active));
        return await Write("PointTransaction_Insert", parameters);
    }

    public async Task<PointTransactionEntity> Update(PointTransactionUpdate request)
    {
        RepositoryValidation.ValidateInput(request);
        return await Write("PointTransaction_Update",
            [
                new("Id", NpgsqlDbType.Uuid, request.Id),
                new("RegistrationId", NpgsqlDbType.Uuid, request.RegistrationId),
                new("Amount", NpgsqlDbType.Integer, request.Amount),
                new("Reason", NpgsqlDbType.Varchar, request.Reason),
                new("GrantedAt", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.GrantedAt)),
                new("Active", NpgsqlDbType.Boolean, request.Active),
                new("PointTransaction", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
            ]);
    }

    private async Task<PointTransactionEntity> Write(string procedure, IReadOnlyList<StoredProcedureParameter> parameters)
    {
        var response = await executor.ExecuteStoredProcedureAsync("grammar", procedure, parameters,
            StoredProcedureResult.Composite("PointTransaction", "grammar", "PointTransaction"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count == 0 || (table.Rows.Count == 1 && table.Rows[0].IsNull("Id")))
            throw new KeyNotFoundException("No se encontró la transacción de puntos.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return ToEntity(table.Rows[0]);
    }

    private static PointTransactionEntity ToEntity(DataRow row)
    {
        return new()
        {
            Id = row.Field<Guid>("Id"), RegistrationId = row.Field<Guid>("RegistrationId"),
            Amount = row.Field<int>("Amount"),
            Reason = row.Field<string>("Reason") ?? throw new DataException("Falta Reason."),
            GrantedAt = row.Field<DateTime>("GrantedAt"), CreatedAt = row.Field<DateTime>("CreatedAt"),
            UpdatedAt = row.Field<DateTime>("UpdatedAt"), Active = row.Field<bool>("Active")
        };
    }
}
