using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.UserLevels;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class UserLevelRepository(IStoredProcedureExecutor executor) : IUserLevelRepository
{
    public async Task<List<UserLevelListItemDto>> List(PaginationDto<UserLevelList> request)
    {
        RepositoryValidation.ValidatePagination(request);
        var filter = request.Filter ?? new UserLevelList();
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "UserLevel_List",
            [
                new("Id", NpgsqlDbType.Uuid, filter.Id),
                new("Level", NpgsqlDbType.Integer, filter.Level),
                new("Name", NpgsqlDbType.Varchar, filter.Name),
                new("MinPoints", NpgsqlDbType.Bigint, filter.MinPoints),
                new("MaxPoints", NpgsqlDbType.Bigint, filter.MaxPoints),
                new("Active", NpgsqlDbType.Boolean, filter.Active),
                new("CreatedAt", NpgsqlDbType.Date, filter.CreatedAt),
                new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
                new("PageSize", NpgsqlDbType.Integer, request.PageSize),
                new("Results", NpgsqlDbType.Refcursor, "userlevel_list", ParameterDirection.InputOutput)
            ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(row => new UserLevelListItemDto
        {
            Id = row.Field<Guid>("Id"), Level = row.Field<int>("Level"),
            Name = row.Field<string>("Name") ?? throw new DataException("Falta Name."),
            MinPoints = row.Field<long>("MinPoints"), CreatedAt = row.Field<DateTime>("CreatedAt"),
            UpdatedAt = row.Field<DateTime>("UpdatedAt"), Active = row.Field<bool>("Active"),
            TotalCount = row.Field<long>("TotalCount")
        }).ToList();
    }
}
