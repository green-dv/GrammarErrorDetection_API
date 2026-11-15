using System.Data;
using System.Text.Json;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class ConditionTypeRepository(IStoredProcedureExecutor executor) : IConditionTypeRepository
{
    public async Task<List<ConditionTypeEntity>> List(PaginationDto<ConditionTypeList> request)
    {
        RepositoryValidation.ValidatePagination(request);
        var filter = request.Filter ?? new ConditionTypeList();
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "ConditionType_List",
        [
            new("Id", NpgsqlDbType.Uuid, filter.Id),
            new("Code", NpgsqlDbType.Varchar, filter.Code),
            new("Name", NpgsqlDbType.Varchar, filter.Name),
            new("Description", NpgsqlDbType.Text, filter.Description),
            new("ValueType", NpgsqlDbType.Varchar, filter.ValueType),
            new("Active", NpgsqlDbType.Boolean, filter.Active),
            new("CreatedAt", NpgsqlDbType.Date, filter.CreatedAt),
            new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
            new("PageSize", NpgsqlDbType.Integer, request.PageSize),
            new("Results", NpgsqlDbType.Refcursor, "gamification_list", ParameterDirection.InputOutput)
        ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(row => Map<ConditionTypeEntity>(row)).ToList();
    }

    public async Task<ConditionTypeEntity> Insert(ConditionTypeInsert request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "ConditionType_Insert",
        [
            new("Code", NpgsqlDbType.Varchar, request.Code),
            new("Name", NpgsqlDbType.Varchar, request.Name),
            new("Description", NpgsqlDbType.Text, request.Description),
            new("ValueType", NpgsqlDbType.Varchar, request.ValueType),
            new("Active", NpgsqlDbType.Boolean, request.Active),
            new("ConditionType", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
        ], StoredProcedureResult.Composite("ConditionType", "grammar", "ConditionType"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return Map<ConditionTypeEntity>(table.Rows[0]);
    }

    public async Task<ConditionTypeEntity> Update(ConditionTypeUpdate request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "ConditionType_Update",
        [
            new("Id", NpgsqlDbType.Uuid, request.Id),
            new("Code", NpgsqlDbType.Varchar, request.Code),
            new("Name", NpgsqlDbType.Varchar, request.Name),
            new("Description", NpgsqlDbType.Text, request.Description),
            new("ValueType", NpgsqlDbType.Varchar, request.ValueType),
            new("Active", NpgsqlDbType.Boolean, request.Active),
            new("ConditionType", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
        ], StoredProcedureResult.Composite("ConditionType", "grammar", "ConditionType"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return Map<ConditionTypeEntity>(table.Rows[0]);
    }

    private static T Map<T>(DataRow row) where T : ConditionTypeEntity, new()
    {
        var item = new T();
        item.Id = row.Field<Guid>("Id");
        item.Code = row.Field<string>("Code")!;
        item.Name = row.Field<string>("Name")!;
        item.Description = row.Field<string?>("Description");
        item.ValueType = row.Field<string>("ValueType")!;
        item.Active = row.Field<bool>("Active");
        item.CreatedAt = row.Field<DateTime>("CreatedAt");
        item.UpdatedAt = row.Field<DateTime>("UpdatedAt");
        return item;
    }

    private static string? Json(JsonElement? value) => value?.GetRawText();
    private static JsonElement? ReadJson(DataRow row, string column)
    {
        if (row.IsNull(column)) return null;
        using var document = JsonDocument.Parse(row.Field<string>(column)!);
        return document.RootElement.Clone();
    }
}
