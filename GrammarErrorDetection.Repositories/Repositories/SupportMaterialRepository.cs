using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
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

    public async Task<List<SupportMaterialListItemDto>> List(PaginationDto<SupportMaterialList> request, Guid? viewerId = null)
    {
        RepositoryValidation.ValidatePagination(request);
        if (viewerId == Guid.Empty) throw new ArgumentException("El usuario que consulta no puede estar vacío.", nameof(viewerId));
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
                new("Results", NpgsqlDbType.Refcursor, "supportmaterial_list", ParameterDirection.InputOutput),
                new("ViewerId", NpgsqlDbType.Uuid, viewerId),
                new("OnlyHelpful", NpgsqlDbType.Boolean, filter.OnlyHelpful)
            ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(ConvertToDto.SupportMaterialListToDto).ToList();
    }

    public async Task<List<SupportMaterialRecommendationRow>> ListRecommendations(Guid userId, Guid? classId,
        int categoryLimit, int materialsPerCategory)
    {
        if (userId == Guid.Empty) throw new ArgumentException("El estudiante no puede estar vacío.", nameof(userId));
        if (classId == Guid.Empty) throw new ArgumentException("El curso no puede estar vacío.", nameof(classId));
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "SupportMaterial_Recommendation_List",
            [
                new("UserId", NpgsqlDbType.Uuid, userId),
                new("ClassId", NpgsqlDbType.Uuid, classId),
                new("CategoryLimit", NpgsqlDbType.Integer, categoryLimit),
                new("MaterialsPerCategory", NpgsqlDbType.Integer, materialsPerCategory),
                new("Results", NpgsqlDbType.Refcursor, "supportmaterial_recommendation_list", ParameterDirection.InputOutput)
            ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(ConvertToDto.SupportMaterialRecommendationToRow).ToList();
    }

    public async Task<SupportMaterialInteractionEntity> RegisterView(Guid userId, Guid supportMaterialId,
        Guid? registrationId, bool downloaded)
    {
        ValidateInteractionKeys(userId, supportMaterialId);
        if (registrationId == Guid.Empty) throw new ArgumentException("La inscripción no puede estar vacía.", nameof(registrationId));
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "SupportMaterialInteraction_RegisterView",
            [
                new("UserId", NpgsqlDbType.Uuid, userId),
                new("SupportMaterialId", NpgsqlDbType.Uuid, supportMaterialId),
                new("SupportMaterialInteraction", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output),
                new("RegistrationId", NpgsqlDbType.Uuid, registrationId),
                new("Downloaded", NpgsqlDbType.Boolean, downloaded)
            ], StoredProcedureResult.Composite("SupportMaterialInteraction", "grammar", "SupportMaterialInteraction"));
        return SingleInteraction(response);
    }

    public async Task<SupportMaterialInteractionEntity> SetFeedback(Guid userId, Guid supportMaterialId, bool helpful, string? comment)
    {
        ValidateInteractionKeys(userId, supportMaterialId);
        if (comment?.Length > 500) throw new ArgumentException("El comentario admite hasta 500 caracteres.", nameof(comment));
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "SupportMaterialInteraction_SetFeedback",
            [
                new("UserId", NpgsqlDbType.Uuid, userId),
                new("SupportMaterialId", NpgsqlDbType.Uuid, supportMaterialId),
                new("Helpful", NpgsqlDbType.Boolean, helpful),
                new("SupportMaterialInteraction", NpgsqlDbType.Unknown, Direction: ParameterDirection.Output),
                new("Comment", NpgsqlDbType.Varchar, comment)
            ], StoredProcedureResult.Composite("SupportMaterialInteraction", "grammar", "SupportMaterialInteraction"));
        return SingleInteraction(response);
    }

    private static void ValidateInteractionKeys(Guid userId, Guid supportMaterialId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("El estudiante no puede estar vacío.", nameof(userId));
        if (supportMaterialId == Guid.Empty) throw new ArgumentException("El material no puede estar vacío.", nameof(supportMaterialId));
    }

    private static SupportMaterialInteractionEntity SingleInteraction(ResourceResponse<DataTable> response)
    {
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        if (table.Rows.Count != 1) throw new DataException("Se esperaba exactamente una interacción.");
        return ConvertToEntity.SupportMaterialInteractionToEntity(table.Rows[0]);
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
