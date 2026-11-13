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

public sealed class AchievementRepository(IStoredProcedureExecutor executor) : IAchievementRepository
{
    public async Task<List<AchievementListItem>> List(PaginationDto<AchievementList> request)
    {
        RepositoryValidation.ValidatePagination(request);
        var filter = request.Filter ?? new AchievementList();
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Achievement_List",
        [
            new("Id", NpgsqlDbType.Uuid, filter.Id),
            new("Name", NpgsqlDbType.Varchar, filter.Name),
            new("Description", NpgsqlDbType.Text, filter.Description),
            new("IconLink", NpgsqlDbType.Varchar, filter.IconLink),
            new("AchievementCategoryId", NpgsqlDbType.Uuid, filter.AchievementCategoryId),
            new("Points", NpgsqlDbType.Integer, filter.Points),
            new("IsSecret", NpgsqlDbType.Boolean, filter.IsSecret),
            new("Active", NpgsqlDbType.Boolean, filter.Active),
            new("CreatedAt", NpgsqlDbType.Date, filter.CreatedAt),
            new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
            new("PageSize", NpgsqlDbType.Integer, request.PageSize),
            new("CreatedByUserId", NpgsqlDbType.Uuid, filter.CreatedByUserId),
            new("AchievementCollectionId", NpgsqlDbType.Uuid, filter.AchievementCollectionId),
            new("TierOrder", NpgsqlDbType.Integer, filter.TierOrder),
            new("Results", NpgsqlDbType.Refcursor, "gamification_list", ParameterDirection.InputOutput),
            new("ConditionTypeId", NpgsqlDbType.Uuid, filter.ConditionTypeId)
        ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(row => Map<AchievementListItem>(row)).ToList();
    }

    public async Task<AchievementEntity> Insert(AchievementInsert request, Guid createdByUserId)
    {
        RepositoryValidation.ValidateInput(request);
        if (createdByUserId == Guid.Empty) throw new ArgumentException("El creador debe ser un usuario autenticado.", nameof(createdByUserId));
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Achievement_Insert",
        [
            new("Name", NpgsqlDbType.Varchar, request.Name),
            new("Description", NpgsqlDbType.Text, request.Description),
            new("AchievementCategoryId", NpgsqlDbType.Uuid, request.AchievementCategoryId),
            new("Conditions", NpgsqlDbType.Jsonb, GamificationJson.Conditions(request.Conditions)),
            new("IconLink", NpgsqlDbType.Varchar, request.IconLink),
            new("Points", NpgsqlDbType.Integer, request.Points),
            new("IsSecret", NpgsqlDbType.Boolean, request.IsSecret),
            new("Active", NpgsqlDbType.Boolean, request.Active),
            new("CreatedByUserId", NpgsqlDbType.Uuid, createdByUserId),
            new("AchievementCollectionId", NpgsqlDbType.Uuid, request.AchievementCollectionId),
            new("TierOrder", NpgsqlDbType.Integer, request.TierOrder),
            new("Achievement", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
        ], StoredProcedureResult.Composite("Achievement", "grammar", "Achievement"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return Map<AchievementEntity>(table.Rows[0]);
    }

    public async Task<AchievementEntity> Update(AchievementUpdate request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Achievement_Update",
        [
            new("Id", NpgsqlDbType.Uuid, request.Id),
            new("Name", NpgsqlDbType.Varchar, request.Name),
            new("Description", NpgsqlDbType.Text, request.Description),
            new("IconLink", NpgsqlDbType.Varchar, request.IconLink),
            new("AchievementCategoryId", NpgsqlDbType.Uuid, request.AchievementCategoryId),
            new("Points", NpgsqlDbType.Integer, request.Points),
            new("IsSecret", NpgsqlDbType.Boolean, request.IsSecret),
            new("Active", NpgsqlDbType.Boolean, request.Active),
            new("CreatedByUserId", NpgsqlDbType.Uuid, null),
            new("AchievementCollectionId", NpgsqlDbType.Uuid, request.AchievementCollectionId),
            new("TierOrder", NpgsqlDbType.Integer, request.TierOrder),
            new("ClearCollection", NpgsqlDbType.Boolean, request.ClearCollection),
            new("Conditions", NpgsqlDbType.Jsonb, GamificationJson.Conditions(request.Conditions)),
            new("Achievement", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
        ], StoredProcedureResult.Composite("Achievement", "grammar", "Achievement"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return Map<AchievementEntity>(table.Rows[0]);
    }

    internal static T Map<T>(DataRow row) where T : AchievementEntity, new()
    {
        var item = new T();
        item.Id = row.Field<Guid>("Id");
        item.AchievementCollectionId = row.Field<Guid?>("AchievementCollectionId");
        item.TierOrder = row.Field<int?>("TierOrder");
        item.Name = row.Field<string>("Name")!;
        item.Description = row.Field<string>("Description")!;
        item.AchievementCategoryId = row.Field<Guid>("AchievementCategoryId");
        item.IconLink = row.Field<string?>("IconLink");
        item.Points = row.Field<int>("Points");
        item.IsSecret = row.Field<bool>("IsSecret");
        item.Active = row.Field<bool>("Active");
        item.CreatedByUserId = row.Field<Guid?>("CreatedByUserId");
        item.CreatedAt = row.Field<DateTime>("CreatedAt");
        item.UpdatedAt = row.Field<DateTime>("UpdatedAt");
        if (item is AchievementListItem detail)
        {
            detail.AchievementCollectionName = row.Field<string?>("AchievementCollectionName");
            detail.AchievementCategoryName = row.Field<string?>("AchievementCategoryName");
            detail.Conditions = GamificationJson.ReadList<GamificationConditionDto>(row, "Conditions");
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
