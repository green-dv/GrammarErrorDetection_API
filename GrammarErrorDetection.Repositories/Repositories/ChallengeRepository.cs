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

public sealed class ChallengeRepository(IStoredProcedureExecutor executor) : IChallengeRepository
{
    public async Task<List<ChallengeListItem>> List(PaginationDto<ChallengeList> request)
    {
        RepositoryValidation.ValidatePagination(request);
        var filter = request.Filter ?? new ChallengeList();
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Challenge_List",
        [
            new("Id", NpgsqlDbType.Uuid, filter.Id),
            new("CreatedByUserId", NpgsqlDbType.Uuid, filter.CreatedByUserId),
            new("Title", NpgsqlDbType.Varchar, filter.Title),
            new("Description", NpgsqlDbType.Text, filter.Description),
            new("Instructions", NpgsqlDbType.Text, filter.Instructions),
            new("Difficulty", NpgsqlDbType.Varchar, filter.Difficulty),
            new("Points", NpgsqlDbType.Integer, filter.Points),
            new("StartDate", NpgsqlDbType.Date, filter.StartDate),
            new("EndDate", NpgsqlDbType.Date, filter.EndDate),
            new("Status", NpgsqlDbType.Varchar, filter.Status),
            new("Active", NpgsqlDbType.Boolean, filter.Active),
            new("CreatedAt", NpgsqlDbType.Date, filter.CreatedAt),
            new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
            new("PageSize", NpgsqlDbType.Integer, request.PageSize),
            new("Results", NpgsqlDbType.Refcursor, "gamification_list", ParameterDirection.InputOutput)
        ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(row => Map<ChallengeListItem>(row)).ToList();
    }

    public async Task<ChallengeEntity> Insert(ChallengeInsert request, Guid createdByUserId)
    {
        RepositoryValidation.ValidateInput(request);
        if (createdByUserId == Guid.Empty) throw new ArgumentException("El creador debe ser un usuario autenticado.", nameof(createdByUserId));
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Challenge_Insert",
        [
            new("CreatedByUserId", NpgsqlDbType.Uuid, createdByUserId),
            new("Title", NpgsqlDbType.Varchar, request.Title),
            new("Description", NpgsqlDbType.Text, request.Description),
            new("Instructions", NpgsqlDbType.Text, request.Instructions),
            new("Difficulty", NpgsqlDbType.Varchar, request.Difficulty),
            new("Points", NpgsqlDbType.Integer, request.Points),
            new("StartDate", NpgsqlDbType.Timestamp, request.StartDate),
            new("EndDate", NpgsqlDbType.Timestamp, request.EndDate),
            new("Status", NpgsqlDbType.Varchar, request.Status),
            new("Active", NpgsqlDbType.Boolean, request.Active),
            new("Conditions", NpgsqlDbType.Jsonb, GamificationJson.Conditions(request.Conditions ?? [])),
            new("Challenge", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
        ], StoredProcedureResult.Composite("Challenge", "grammar", "Challenge"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return Map<ChallengeEntity>(table.Rows[0]);
    }

    public async Task<ChallengeEntity> Update(ChallengeUpdate request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Challenge_Update",
        [
            new("Id", NpgsqlDbType.Uuid, request.Id),
            new("CreatedByUserId", NpgsqlDbType.Uuid, null),
            new("Title", NpgsqlDbType.Varchar, request.Title),
            new("Description", NpgsqlDbType.Text, request.Description),
            new("Instructions", NpgsqlDbType.Text, request.Instructions),
            new("Difficulty", NpgsqlDbType.Varchar, request.Difficulty),
            new("Points", NpgsqlDbType.Integer, request.Points),
            new("StartDate", NpgsqlDbType.Timestamp, request.StartDate),
            new("EndDate", NpgsqlDbType.Timestamp, request.EndDate),
            new("Status", NpgsqlDbType.Varchar, request.Status),
            new("Active", NpgsqlDbType.Boolean, request.Active),
            new("Challenge", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output),
            new("Conditions", NpgsqlDbType.Jsonb, GamificationJson.Conditions(request.Conditions))
        ], StoredProcedureResult.Composite("Challenge", "grammar", "Challenge"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return Map<ChallengeEntity>(table.Rows[0]);
    }

    public async Task<ChallengeEntity> Repeat(Guid id, ChallengeRepeat request)
    {
        if (id == Guid.Empty) throw new ArgumentException("El desafío no puede estar vacío.", nameof(id));
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Challenge_Repeat",
        [
            new("Id", NpgsqlDbType.Uuid, id),
            new("StartDate", NpgsqlDbType.Timestamp, request.StartDate),
            new("Challenge", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output),
            new("EndDate", NpgsqlDbType.Timestamp, request.EndDate)
        ], StoredProcedureResult.Composite("Challenge", "grammar", "Challenge"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return Map<ChallengeEntity>(table.Rows[0]);
    }

    public async Task<ChallengeEntity> Duplicate(Guid id, Guid createdByUserId)
    {
        if (id == Guid.Empty) throw new ArgumentException("El desafío no puede estar vacío.", nameof(id));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("El creador debe ser un usuario autenticado.", nameof(createdByUserId));
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Challenge_Duplicate",
        [
            new("Id", NpgsqlDbType.Uuid, id),
            new("CreatedByUserId", NpgsqlDbType.Uuid, createdByUserId),
            new("Challenge", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
        ], StoredProcedureResult.Composite("Challenge", "grammar", "Challenge"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return Map<ChallengeEntity>(table.Rows[0]);
    }

    private static T Map<T>(DataRow row) where T : ChallengeEntity, new()
    {
        var item = new T();
        item.Id = row.Field<Guid>("Id");
        item.CreatedByUserId = row.Field<Guid>("CreatedByUserId");
        item.Title = row.Field<string>("Title")!;
        item.Description = row.Field<string>("Description")!;
        item.Instructions = row.Field<string?>("Instructions");
        item.Difficulty = row.Field<string>("Difficulty")!;
        item.Points = row.Field<int>("Points");
        item.StartDate = row.Field<DateTime?>("StartDate");
        item.EndDate = row.Field<DateTime?>("EndDate");
        item.Status = row.Field<string>("Status")!;
        item.Active = row.Field<bool>("Active");
        item.CreatedAt = row.Field<DateTime>("CreatedAt");
        item.UpdatedAt = row.Field<DateTime>("UpdatedAt");
        item.Round = row.Field<int>("Round");
        if (item is ChallengeListItem detail)
        {
            detail.Conditions = GamificationJson.ReadList<GamificationConditionDto>(row, "Conditions");
            detail.CompletionCount = row.Field<long>("CompletionCount");
        }
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
