using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class SupportMaterialRepository : ISupportMaterialRepository
{
    private readonly IStoredProcedureExecutor executor;

    public SupportMaterialRepository(IStoredProcedureExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);
        this.executor = executor;
    }

    public async Task<List<SupportMaterialListItemDto>> List(PaginationDto<SupportMaterialList> request)
    {
        RepositoryValidation.ValidatePagination(request);
        var filter = request.Filter ?? new SupportMaterialList();
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "SupportMaterial_List",
            [
                new("Id", NpgsqlDbType.Uuid, filter.Id),
                new("Name", NpgsqlDbType.Varchar, filter.Name),
                new("ErrorClassificationId", NpgsqlDbType.Uuid, filter.ErrorClassificationId),
                new("UploadDate", NpgsqlDbType.Date, filter.UploadDate),
                new("FileLink", NpgsqlDbType.Text, filter.FileLink),
                new("FileTypeId", NpgsqlDbType.Uuid, filter.FileTypeId),
                new("FileSize", NpgsqlDbType.Bigint, filter.FileSize),
                new("UploadedBy", NpgsqlDbType.Uuid, filter.UploadedBy),
                new("Active", NpgsqlDbType.Boolean, filter.Active),
                new("CreatedAt", NpgsqlDbType.Date, filter.CreatedAt),
                new("PageNumber", NpgsqlDbType.Integer, request.PageNumber),
                new("PageSize", NpgsqlDbType.Integer, request.PageSize),
                new("Results", NpgsqlDbType.Refcursor, "supportmaterial_list", ParameterDirection.InputOutput)
            ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(ConvertToDto.SupportMaterialListToDto).ToList();
    }

    public async Task<SupportMaterialEntity> Insert(SupportMaterialInsert request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "SupportMaterial_Insert",
            [
                new("Name", NpgsqlDbType.Varchar, request.Name),
                new("ErrorClassificationId", NpgsqlDbType.Uuid, request.ErrorClassificationId),
                new("UploadDate", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.UploadDate)),
                new("FileLink", NpgsqlDbType.Text, request.FileLink),
                new("FileTypeId", NpgsqlDbType.Uuid, request.FileTypeId),
                new("UploadedBy", NpgsqlDbType.Uuid, request.UploadedBy),
                new("SupportMaterial", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output),
                new("FileSize", NpgsqlDbType.Bigint, request.FileSize),
                new("Active", NpgsqlDbType.Boolean, request.Active)
            ], StoredProcedureResult.Composite("SupportMaterial", "grammar", "SupportMaterial"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return ConvertToEntity.SupportMaterialToEntity(table.Rows[0]);
    }

    public async Task<SupportMaterialEntity> Update(SupportMaterialUpdate request)
    {
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "SupportMaterial_Update",
            [
                new("Id", NpgsqlDbType.Uuid, request.Id),
                new("Name", NpgsqlDbType.Varchar, request.Name),
                new("ErrorClassificationId", NpgsqlDbType.Uuid, request.ErrorClassificationId),
                new("UploadDate", NpgsqlDbType.Timestamp, DatabaseDateTime.ToTimestamp(request.UploadDate)),
                new("FileLink", NpgsqlDbType.Text, request.FileLink),
                new("FileTypeId", NpgsqlDbType.Uuid, request.FileTypeId),
                new("FileSize", NpgsqlDbType.Bigint, request.FileSize),
                new("UploadedBy", NpgsqlDbType.Uuid, request.UploadedBy),
                new("Active", NpgsqlDbType.Boolean, request.Active),
                new("SupportMaterial", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output)
            ], StoredProcedureResult.Composite("SupportMaterial", "grammar", "SupportMaterial"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una entidad.");
        return ConvertToEntity.SupportMaterialToEntity(table.Rows[0]);
    }
}
