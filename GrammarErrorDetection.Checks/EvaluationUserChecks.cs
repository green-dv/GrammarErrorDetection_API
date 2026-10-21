using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Evaluations;
using GrammarErrorDetection.Repositories.Repositories;
using GrammarErrorDetection.Services.Services;
using NpgsqlTypes;

internal static class EvaluationUserChecks
{
    public static async Task Run()
    {
        var executor = new EvaluationExecutor();
        var service = new EvaluationService(new EvaluationRepository(executor));
        var data = await service.List(new PaginationDto<EvaluationList>
        {
            PageNumber = 1, PageSize = 20,
            Filter = new() { UserId = executor.UserId, RegistrationId = executor.RegistrationId, Period = 1 }
        });
        Check(executor.Procedure == "Evaluation_List"
            && executor.Parameters.Single(p => p.Name == "UserId") is { Type: NpgsqlDbType.Uuid } userParameter
            && Equals(userParameter.Value, executor.UserId), "El servicio y repositorio envían UserId como UUID al SP de listado.");
        Check(data.Single().UserId == executor.UserId && data[0].RegistrationId == executor.RegistrationId
            && data[0].WritingScore == 0 && data[0].OralScore is null, "Mapea UserId sin cambiar notas cero o ausentes.");
        Check(Json(data[0]).GetProperty("userId").GetGuid() == executor.UserId, "La respuesta del listado incluye userId.");
        await service.List(new() { PageNumber = 1, PageSize = 20 });
        Check(executor.Parameters.Single(p => p.Name == "UserId").Value is null, "Omitir UserId conserva el listado sin ese filtro.");
        try
        {
            await service.List(new() { PageNumber = 1, PageSize = 20, Filter = new() { UserId = Guid.Empty } });
            throw new Exception("Se aceptó UserId vacío.");
        }
        catch (ValidationException) { }
        var inserted = await service.Insert(new() { RegistrationId = executor.RegistrationId, Period = 1 });
        Check(executor.Procedure == "Evaluation_Insert" && executor.Parameters.All(p => p.Name != "UserId")
            && inserted.UserId is null && !Json(inserted).TryGetProperty("userId", out _),
            "Insert no envía UserId, no exige su columna y no lo devuelve en JSON.");
        var updated = await service.Update(new() { Id = inserted.Id, WritingScore = 0 });
        Check(executor.Procedure == "Evaluation_Update" && executor.Parameters.All(p => p.Name != "UserId")
            && updated.UserId is null && !Json(updated).TryGetProperty("userId", out _),
            "Update no envía UserId, no exige su columna y no lo devuelve en JSON.");
        Check(typeof(EvaluationInsert).GetProperty("UserId") is null && typeof(EvaluationUpdate).GetProperty("UserId") is null,
            "Los contratos de escritura no incluyen UserId.");
        Console.WriteLine("8 comprobaciones de UserId en evaluaciones correctas.");
    }

    private static JsonElement Json(EvaluationEntity evaluation) =>
        JsonSerializer.SerializeToElement(evaluation, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class EvaluationExecutor : IStoredProcedureExecutor
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid RegistrationId { get; } = Guid.NewGuid();
        public string? Procedure { get; private set; }
        public IReadOnlyList<StoredProcedureParameter> Parameters { get; private set; } = [];
        public Task<ResourceResponse<DataTable>> ExecuteStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, StoredProcedureResult? result = null)
        {
            Check(schema == "grammar", "Usa el esquema grammar.");
            Procedure = procedureName;
            Parameters = parameters;
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("RegistrationId", typeof(Guid));
            foreach (var field in new[] { "Period", "WritingScore", "GrammarScore", "VocabularyScore", "ReadingScore",
                "ListeningScore", "OralScore", "AcademicPerformance" })
                table.Columns.Add(field, typeof(int));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            table.Columns.Add("UpdatedAt", typeof(DateTime));
            table.Columns.Add("Active", typeof(bool));
            if (procedureName == "Evaluation_List") table.Columns.Add("UserId", typeof(Guid));
            var row = table.NewRow();
            row["Id"] = Guid.NewGuid();
            row["RegistrationId"] = RegistrationId;
            row["Period"] = 1;
            row["WritingScore"] = 0;
            row["CreatedAt"] = new DateTime(2026, 10, 20);
            row["UpdatedAt"] = new DateTime(2026, 10, 20);
            row["Active"] = true;
            if (procedureName == "Evaluation_List") row["UserId"] = UserId;
            table.Rows.Add(row);
            return Task.FromResult(ResourceResponse<DataTable>.Ok(table));
        }
    }
}
