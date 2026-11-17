using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.Grammar.ErrorClassifications;
using GrammarErrorDetection.Repositories.Interfaces;
using NpgsqlTypes;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Repositories.Utils;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class ErrorClassificationRepository(IStoredProcedureExecutor writer, IStoredProcedurePageExecutor executor) : IErrorClassificationRepository
{
    public async Task<PagedList<ErrorClassificationEntity>> List(PaginationDto<ErrorClassificationList> request)
    {
        RepositoryValidation.ValidatePagination(request);
        var filter = request.Filter ?? new ErrorClassificationList();
        return await ListPage(filter.Id, filter.Name, filter.Description, filter.Active, filter.CreatedAt,
            request.PageNumber, request.PageSize);
    }

    public async Task<List<ErrorClassificationEntity>> FindByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var matches = new List<ErrorClassificationEntity>();
        const int pageSize = 200;
        for (var page = 1; ; page++)
        {
            var result = await ListPage(null, name.Trim(), null, true, null, page, pageSize);
            matches.AddRange(result.Items.Where(classification => classification.Active
                && string.Equals(classification.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)));
            if ((long)page * pageSize >= result.TotalCount) return matches;
        }
    }

    public async Task<ErrorClassificationEntity> Insert(ErrorClassificationInsert request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await writer.ExecuteStoredProcedureAsync("grammar", "ErrorClassification_Insert",
            [
                new("Name", NpgsqlDbType.Varchar, request.Name),
                new("Description", NpgsqlDbType.Text, request.Description),
                new("Active", NpgsqlDbType.Boolean, request.Active),
                new("ErrorClassification", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
            ], StoredProcedureResult.Composite("ErrorClassification", "grammar", "ErrorClassification"));
        return ReadSingle(response.GetDataOrThrow());
    }

    public async Task<ErrorClassificationEntity> Update(ErrorClassificationUpdate request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await writer.ExecuteStoredProcedureAsync("grammar", "ErrorClassification_Update",
            [
                new("Id", NpgsqlDbType.Uuid, request.Id),
                new("Name", NpgsqlDbType.Varchar, request.Name),
                new("Description", NpgsqlDbType.Text, request.Description),
                new("Active", NpgsqlDbType.Boolean, request.Active),
                new("ErrorClassification", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
            ], StoredProcedureResult.Composite("ErrorClassification", "grammar", "ErrorClassification"));
        return ReadSingle(response.GetDataOrThrow());
    }

    private static ErrorClassificationEntity ReadSingle(DataTable? table)
    {
        using var data = table ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (data.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return Map(data.Rows[0]);
    }

    private static ErrorClassificationEntity Map(DataRow row) => new()
    {
        Id = row.Field<Guid>("Id"),
        Name = row.Field<string>("Name") ?? throw new DataException("Falta Name."),
        Description = row.Field<string>("Description"),
        CreatedAt = row.Field<DateTime>("CreatedAt"),
        UpdatedAt = row.Field<DateTime>("UpdatedAt"),
        Active = row.Field<bool>("Active")
    };

    // Una sola llamada al SP devuelve la página filtrada ("Results") y el total sin paginar ("Total").
    private async Task<PagedList<ErrorClassificationEntity>> ListPage(Guid? id, string? name, string? description,
        bool? active, DateOnly? createdAt, int pageNumber, int pageSize)
    {
        var response = await executor.ExecutePagedStoredProcedureAsync("grammar", "ErrorClassification_List",
            [
                new("Id", NpgsqlDbType.Uuid, id),
                new("Name", NpgsqlDbType.Varchar, name),
                new("Description", NpgsqlDbType.Text, description),
                new("Active", NpgsqlDbType.Boolean, active),
                new("CreatedAt", NpgsqlDbType.Date, createdAt),
                new("PageNumber", NpgsqlDbType.Integer, pageNumber),
                new("PageSize", NpgsqlDbType.Integer, pageSize),
                new("Results", NpgsqlDbType.Refcursor, "errorclassification_list", ParameterDirection.InputOutput),
                new("Total", NpgsqlDbType.Refcursor, "errorclassification_total", ParameterDirection.InputOutput)
            ]);
        using var page = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió la página.");
        var items = page.Rows.AsEnumerable().Select(Map).ToList();
        return new PagedList<ErrorClassificationEntity>(items, page.TotalCount);
    }
}
