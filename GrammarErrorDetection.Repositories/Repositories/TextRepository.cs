using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Texts;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class TextRepository : ITextRepository
{
    private readonly IStoredProcedureExecutor executor;

    public TextRepository(IStoredProcedureExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);
        this.executor = executor;
    }

    public async Task<List<TextListItemDto>> List(PaginationDto<TextList> request)
    {
        RepositoryValidation.ValidatePagination(request);
        var filter = request.Filter ?? new TextList();
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Text_Detail_List",
            [
                new("Id", NpgsqlDbType.Uuid, filter.Id),
                new("UserId", NpgsqlDbType.Uuid, filter.UserId),
                new("SubmissionDate", NpgsqlDbType.Date, filter.SubmissionDate),
                new("OriginalContent", NpgsqlDbType.Text, filter.OriginalContent),
                new("Status", NpgsqlDbType.Varchar, filter.Status),
                new("Active", NpgsqlDbType.Boolean, filter.Active),
                new("CreatedAt", NpgsqlDbType.Date, filter.CreatedAt),
                new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
                new("PageSize", NpgsqlDbType.Integer, request.PageSize),
                new("Results", NpgsqlDbType.Refcursor, "text_detail_list", ParameterDirection.InputOutput)
            ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(ConvertToDto.TextListToDto).ToList();
    }

    public async Task<TextEntity> Insert(TextInsert request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Text_Insert",
            [
                new("UserId", NpgsqlDbType.Uuid, request.UserId),
                new("OriginalContent", NpgsqlDbType.Text, request.OriginalContent),
                new("Text", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output),
                new("SubmissionDate", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.SubmissionDate)),
                new("Status", NpgsqlDbType.Varchar, request.Status),
                new("Active", NpgsqlDbType.Boolean, request.Active)
            ], StoredProcedureResult.Composite("Text", "grammar", "Text"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return ConvertToEntity.TextToEntity(table.Rows[0]);
    }

    public async Task<TextEntity> Update(TextUpdate request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "Text_Update",
            [
                new("Id", NpgsqlDbType.Uuid, request.Id),
                new("UserId", NpgsqlDbType.Uuid, request.UserId),
                new("SubmissionDate", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.SubmissionDate)),
                new("OriginalContent", NpgsqlDbType.Text, request.OriginalContent),
                new("Status", NpgsqlDbType.Varchar, request.Status),
                new("Active", NpgsqlDbType.Boolean, request.Active),
                new("Text", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
            ], StoredProcedureResult.Composite("Text", "grammar", "Text"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return ConvertToEntity.TextToEntity(table.Rows[0]);
    }
}
