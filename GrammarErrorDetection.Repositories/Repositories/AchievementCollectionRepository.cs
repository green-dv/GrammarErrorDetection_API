using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class AchievementCollectionRepository(IStoredProcedureExecutor executor) : IAchievementCollectionRepository
{
    public async Task<List<AchievementCollectionEntity>> List(PaginationDto<AchievementCollectionList> request)
    {
        RepositoryValidation.ValidatePagination(request);
        var filter = request.Filter ?? new AchievementCollectionList();
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "AchievementCollection_List",
        [
            new("Id", NpgsqlDbType.Uuid, filter.Id),
            new("Name", NpgsqlDbType.Varchar, filter.Name),
            new("CreatedByUserId", NpgsqlDbType.Uuid, filter.CreatedByUserId),
            new("Active", NpgsqlDbType.Boolean, filter.Active),
            new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
            new("PageSize", NpgsqlDbType.Integer, request.PageSize),
            new("Results", NpgsqlDbType.Refcursor, "achievementcollection_list", ParameterDirection.InputOutput)
        ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(Map).ToList();
    }

    public async Task<AchievementCollectionEntity> Insert(AchievementCollectionInsert request, Guid createdByUserId)
    {
        RepositoryValidation.ValidateInput(request);
        if (createdByUserId == Guid.Empty) throw new ArgumentException("El creador debe ser un usuario autenticado.", nameof(createdByUserId));
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "AchievementCollection_Insert",
        [
            new("Name", NpgsqlDbType.Varchar, request.Name),
            new("CreatedByUserId", NpgsqlDbType.Uuid, createdByUserId),
            new("Description", NpgsqlDbType.Text, request.Description),
            new("Active", NpgsqlDbType.Boolean, request.Active),
            new("AchievementCollection", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
        ], StoredProcedureResult.Composite("AchievementCollection", "grammar", "AchievementCollection"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return Map(table.Rows[0]);
    }

    public async Task<AchievementCollectionEntity> Update(AchievementCollectionUpdate request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "AchievementCollection_Update",
        [
            new("Id", NpgsqlDbType.Uuid, request.Id),
            new("Name", NpgsqlDbType.Varchar, request.Name),
            new("Description", NpgsqlDbType.Text, request.Description),
            new("Active", NpgsqlDbType.Boolean, request.Active),
            new("AchievementCollection", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
        ], StoredProcedureResult.Composite("AchievementCollection", "grammar", "AchievementCollection"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return Map(table.Rows[0]);
    }

    private static AchievementCollectionEntity Map(DataRow row) => new()
    {
        Id = row.Field<Guid>("Id"),
        Name = row.Field<string>("Name")!,
        Description = row.Field<string?>("Description"),
        CreatedByUserId = row.Field<Guid>("CreatedByUserId"),
        CreatedAt = row.Field<DateTime>("CreatedAt"),
        UpdatedAt = row.Field<DateTime>("UpdatedAt"),
        Active = row.Field<bool>("Active")
    };
}
