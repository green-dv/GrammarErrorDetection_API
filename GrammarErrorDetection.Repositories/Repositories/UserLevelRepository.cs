using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.Grammar.UserLevels;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class UserLevelRepository(IStoredProcedureExecutor writer, IStoredProcedurePageExecutor pages) : IUserLevelRepository
{
    public async Task<PagedList<UserLevelListItemDto>> List(PaginationDto<UserLevelList> request)
    {
        RepositoryValidation.ValidatePagination(request);
        var filter = request.Filter ?? new UserLevelList();
        // Una sola llamada: la página filtrada ("Results") y el total sin paginar ("Total").
        var response = await pages.ExecutePagedStoredProcedureAsync("grammar", "UserLevel_List",
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
                new("Results", NpgsqlDbType.Refcursor, "userlevel_list", ParameterDirection.InputOutput),
                new("Total", NpgsqlDbType.Refcursor, "userlevel_total", ParameterDirection.InputOutput)
            ]);
        using var page = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió la página.");
        var total = page.TotalCount;
        // TotalCount también viaja en cada fila: la WEB de puntos lo lee de ahí.
        var items = page.Rows.AsEnumerable().Select(row => Map(row, total)).ToList();
        return new PagedList<UserLevelListItemDto>(items, total);
    }

    public async Task<UserLevelListItemDto> Insert(UserLevelInsert request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await writer.ExecuteStoredProcedureAsync("grammar", "UserLevel_Insert",
            [
                new("Level", NpgsqlDbType.Integer, request.Level),
                new("Name", NpgsqlDbType.Varchar, request.Name),
                new("MinPoints", NpgsqlDbType.Bigint, request.MinPoints),
                new("Active", NpgsqlDbType.Boolean, request.Active),
                new("UserLevel", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
            ], StoredProcedureResult.Composite("UserLevel", "grammar", "UserLevel"));
        return ReadSingle(response.GetDataOrThrow());
    }

    public async Task<UserLevelListItemDto> Update(UserLevelUpdate request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await writer.ExecuteStoredProcedureAsync("grammar", "UserLevel_Update",
            [
                new("Id", NpgsqlDbType.Uuid, request.Id),
                new("Level", NpgsqlDbType.Integer, request.Level),
                new("Name", NpgsqlDbType.Varchar, request.Name),
                new("MinPoints", NpgsqlDbType.Bigint, request.MinPoints),
                new("Active", NpgsqlDbType.Boolean, request.Active),
                new("UserLevel", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
            ], StoredProcedureResult.Composite("UserLevel", "grammar", "UserLevel"));
        return ReadSingle(response.GetDataOrThrow());
    }

    private static UserLevelListItemDto ReadSingle(DataTable? table)
    {
        using var data = table ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (data.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return Map(data.Rows[0], 0);
    }

    // El alta y la edición devuelven la fila de la tabla (sin UserCount ni TotalCount).
    private static UserLevelListItemDto Map(DataRow row, long total) => new()
    {
        Id = row.Field<Guid>("Id"),
        Level = row.Field<int>("Level"),
        Name = row.Field<string>("Name") ?? throw new DataException("Falta Name."),
        MinPoints = row.Field<long>("MinPoints"),
        CreatedAt = row.Field<DateTime>("CreatedAt"),
        UpdatedAt = row.Field<DateTime>("UpdatedAt"),
        Active = row.Field<bool>("Active"),
        UserCount = row.Table.Columns.Contains("UserCount") ? row.Field<long>("UserCount") : 0,
        TotalCount = total
    };
}
