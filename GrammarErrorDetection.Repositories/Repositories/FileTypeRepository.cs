using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.FileTypes;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class FileTypeRepository(IStoredProcedureExecutor executor) : IFileTypeRepository
{
    public async Task<List<FileTypeEntity>> List(PaginationDto<FileTypeList> request)
    {
        RepositoryValidation.ValidatePagination(request);
        var filter = request.Filter ?? new FileTypeList();
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "FileType_List",
            [
                new("Id", NpgsqlDbType.Uuid, filter.Id),
                new("Name", NpgsqlDbType.Varchar, filter.Name),
                new("Extension", NpgsqlDbType.Varchar, filter.Extension),
                new("MimeType", NpgsqlDbType.Varchar, filter.MimeType),
                new("Active", NpgsqlDbType.Boolean, filter.Active),
                new("CreatedAt", NpgsqlDbType.Date, filter.CreatedAt),
                new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
                new("PageSize", NpgsqlDbType.Integer, request.PageSize),
                new("Results", NpgsqlDbType.Refcursor, "filetype_list", ParameterDirection.InputOutput)
            ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(row => new FileTypeEntity
        {
            Id = row.Field<Guid>("Id"),
            Name = row.Field<string>("Name") ?? throw new DataException("Falta Name."),
            Extension = row.Field<string>("Extension") ?? throw new DataException("Falta Extension."),
            MimeType = row.Field<string>("MimeType") ?? throw new DataException("Falta MimeType."),
            CreatedAt = row.Field<DateTime>("CreatedAt"),
            UpdatedAt = row.Field<DateTime>("UpdatedAt"),
            Active = row.Field<bool>("Active")
        }).ToList();
    }
}
