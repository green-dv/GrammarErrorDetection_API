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

public sealed class UserAchievementRepository(IStoredProcedureExecutor executor) : IUserAchievementRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<List<UserAchievementBestItem>> ListBest(Guid userId, PaginationDto<UserAchievementBestList> request)
    {
        RepositoryValidation.ValidatePagination(request);
        if (userId == Guid.Empty) throw new ArgumentException("El usuario es obligatorio.", nameof(userId));
        var filter = request.Filter ?? new UserAchievementBestList();
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "UserAchievement_Best_List",
        [
            new("UserId", NpgsqlDbType.Uuid, userId),
            new("AchievementCollectionId", NpgsqlDbType.Uuid, filter.AchievementCollectionId),
            new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
            new("PageSize", NpgsqlDbType.Integer, request.PageSize),
            new("Results", NpgsqlDbType.Refcursor, "userachievement_best_list", ParameterDirection.InputOutput),
            new("AchievementCategoryId", NpgsqlDbType.Uuid, filter.AchievementCategoryId)
        ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(Map).ToList();
    }

    private static UserAchievementBestItem Map(DataRow row)
    {
        var item = AchievementRepository.Map<UserAchievementBestItem>(row);
        item.AchievementCollectionName = row.Field<string?>("AchievementCollectionName");
        item.AchievementCollectionDescription = row.Field<string?>("AchievementCollectionDescription");
        item.AchievementCategoryName = row.Field<string?>("AchievementCategoryName");
        item.ConditionCode = row.Field<string?>("ConditionCode");
        item.ConditionName = row.Field<string?>("ConditionName");
        item.ConditionCount = row.Field<int>("ConditionCount");
        item.Conditions = GrammarErrorDetection.Repositories.Utils.GamificationJson.ReadList<GamificationConditionProgress>(row, "Conditions");
        item.IsObtained = row.Field<bool>("IsObtained");
        item.IsMaxTier = row.Field<bool>("IsMaxTier");
        item.HasNextAchievement = row.Field<bool>("HasNextAchievement");
        item.UserAchievementId = row.Field<Guid?>("UserAchievementId");
        item.ObtainedAt = row.Field<DateTime?>("ObtainedAt");
        item.PointTransactionId = row.Field<Guid?>("PointTransactionId");
        item.AwardPoints = row.Field<int?>("AwardPoints");
        item.BestAchievement = Detail(row, "BestAchievement");
        item.NextAchievement = Detail(row, "NextAchievement");
        item.CurrentValue = row.Field<decimal?>("CurrentValue");
        item.NextTargetValue = row.Field<decimal?>("NextTargetValue");
        item.NextComparisonOperator = row.Field<string?>("NextComparisonOperator");
        item.NextRequiredValue = row.Field<decimal?>("NextRequiredValue");
        item.RemainingValue = row.Field<decimal?>("RemainingValue");
        item.ProgressPercent = row.Field<decimal?>("ProgressPercent");
        item.NextConditionMet = row.Field<bool?>("NextConditionMet");
        item.CanReachNext = row.Field<bool?>("CanReachNext");
        item.TotalCount = row.Field<long>("TotalCount");
        return item;
    }

    private static UserAchievementDetail? Detail(DataRow row, string column)
        => row.IsNull(column) ? null : JsonSerializer.Deserialize<UserAchievementDetail>(row.Field<string>(column)!, JsonOptions);
}
