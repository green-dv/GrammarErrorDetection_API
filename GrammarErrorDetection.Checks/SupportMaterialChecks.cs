using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;
using GrammarErrorDetection.Repositories.Repositories;
using GrammarErrorDetection.Services.Services;
using NpgsqlTypes;

internal static class SupportMaterialChecks
{
    public static async Task Run()
    {
        var checks = 0;
        var executor = new MaterialExecutor();
        var service = new SupportMaterialService(new SupportMaterialRepository(executor));
        var date = new DateOnly(2026, 10, 21);
        var rows = await service.List(new()
        {
            PageNumber = 2, PageSize = 10, Filter = new() { Name = "Guía", ErrorClassificationId = executor.CategoryId,
                UploadDate = date, FileTypeId = executor.FileTypeId, FileSize = 5_000_000_000L,
                UploadedBy = executor.UploaderId, Active = false, CreatedAt = date }
        });
        Check(executor.Procedure == "SupportMaterial_List" && executor.Parameters.Select(p => p.Name).SequenceEqual(
            ["Id", "Name", "ErrorClassificationId", "UploadDate", "FileLink", "FileTypeId", "FileSize", "UploadedBy", "Active", "CreatedAt", "PageNumber", "PageSize", "Results"]),
            "El listado usa exactamente la firma del SP.");
        Check(executor.Parameters.Single(p => p.Name == "Results").Direction == ParameterDirection.InputOutput
            && executor.Parameters.Single(p => p.Name == "UploadDate").Type == NpgsqlDbType.Date
            && executor.Parameters.Single(p => p.Name == "FileSize").Type == NpgsqlDbType.Bigint,
            "Usa cursor INOUT, fechas DATE y tamaño BIGINT.");
        Check(rows.Single().ErrorClassificationName == "Concordancia" && rows[0].UploadedByName == "Ana Pérez"
            && rows[0].FileSize == 5_000_000_000L && !rows[0].Active,
            "El listado incluye ambos nombres y conserva tamaños mayores a 32 bits y Active=false.");
        await service.List(new() { PageNumber = 1, PageSize = 20 });
        Check(executor.Parameters.Take(10).All(p => p.Value is null), "Los filtros omitidos no se sustituyen por valores por defecto.");
        var single = await service.Get(executor.MaterialId);
        Check(single.Id == executor.MaterialId && Equals(executor.Parameters.Single(p => p.Name == "Id").Value, executor.MaterialId)
            && Equals(executor.Parameters.Single(p => p.Name == "PageSize").Value, 1), "Obtener por ID reutiliza SupportMaterial_List.");
        var inserted = await service.Insert(new SupportMaterialInsert
        {
            Name = "Guía", ErrorClassificationId = executor.CategoryId, UploadDate = new DateTime(2026, 10, 21, 12, 30, 0, DateTimeKind.Utc),
            FileLink = "/materials/guide.pdf", FileTypeId = executor.FileTypeId, UploadedBy = executor.UploaderId
        });
        Check(executor.Procedure == "SupportMaterial_Insert" && executor.Parameters.Single(p => p.Name == "SupportMaterial").Direction == ParameterDirection.Output
            && executor.Parameters.Single(p => p.Name == "FileSize").Value is null
            && Equals(executor.Parameters.Single(p => p.Name == "Active").Value, true),
            "Insert usa el compuesto OUT y conserva FileSize=null y Active=true por defecto.");
        Check(Equals(executor.Parameters.Single(p => p.Name == "UploadDate").Value, new DateTime(2026, 10, 21, 8, 30, 0)),
            "Normaliza UploadDate UTC a TIMESTAMP de Bolivia.");
        Check(inserted.Id == executor.MaterialId && inserted.FileSize is null
            && !Json(inserted).TryGetProperty("errorClassificationName", out _)
            && !Json(inserted).TryGetProperty("uploadedByName", out _),
            "Insert mapea la entidad sin exigir las columnas adicionales del listado.");
        await service.Update(new() { Id = executor.MaterialId });
        Check(executor.Procedure == "SupportMaterial_Update" && executor.Parameters
            .Where(p => p.Direction == ParameterDirection.Input && p.Name != "Id").All(p => p.Value is null),
            "Update con solo Id envía null para conservar todos los campos actuales.");
        var updated = await service.Update(new() { Id = executor.MaterialId, FileSize = 0, Active = false });
        Check(updated.FileSize == 0 && !updated.Active, "Update conserva cero y false, sin confundirlos con campos omitidos.");
        executor.ReturnEmpty = true;
        try { await service.Get(executor.MaterialId); throw new Exception("No devolvió 404 para material inexistente."); }
        catch (KeyNotFoundException) { checks++; }
        executor.ReturnEmpty = false;
        await Reject(() => service.Insert(new() { Name = "Guía" }));
        await Reject(() => service.Update(new() { Id = executor.MaterialId, FileSize = -1 }));
        await Reject(() => service.Update(new() { Id = executor.MaterialId, Name = new string('a', 256) }));
        Console.WriteLine($"{checks} comprobaciones del CRUD de material de apoyo correctas.");

        void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
        async Task Reject(Func<Task> action)
        {
            try { await action(); }
            catch (ValidationException) { checks++; return; }
            throw new Exception("Se aceptaron datos inválidos.");
        }
    }

    private static JsonElement Json(SupportMaterialEntity material) => JsonSerializer.SerializeToElement(material, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private sealed class MaterialExecutor : IStoredProcedureExecutor
    {
        public Guid MaterialId { get; } = Guid.NewGuid();
        public Guid CategoryId { get; } = Guid.NewGuid();
        public Guid FileTypeId { get; } = Guid.NewGuid();
        public Guid UploaderId { get; } = Guid.NewGuid();
        public bool ReturnEmpty { get; set; }
        public string? Procedure { get; private set; }
        public IReadOnlyList<StoredProcedureParameter> Parameters { get; private set; } = [];

        public Task<ResourceResponse<DataTable>> ExecuteStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, StoredProcedureResult? result = null)
        {
            if (schema != "grammar" || result is null) throw new Exception("Contrato de ejecución inválido.");
            Procedure = procedureName;
            Parameters = parameters;
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("ErrorClassificationId", typeof(Guid));
            table.Columns.Add("UploadDate", typeof(DateTime));
            table.Columns.Add("FileLink", typeof(string));
            table.Columns.Add("FileTypeId", typeof(Guid));
            table.Columns.Add("FileSize", typeof(long));
            table.Columns.Add("UploadedBy", typeof(Guid));
            table.Columns.Add("Active", typeof(bool));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            table.Columns.Add("UpdatedAt", typeof(DateTime));
            var isList = procedureName == "SupportMaterial_List";
            if (isList)
            {
                table.Columns.Add("ErrorClassificationName", typeof(string));
                table.Columns.Add("UploadedByName", typeof(string));
            }
            if (ReturnEmpty) return Task.FromResult(ResourceResponse<DataTable>.Ok(table));
            var row = table.NewRow();
            row["Id"] = MaterialId;
            row["Name"] = "Guía";
            row["ErrorClassificationId"] = CategoryId;
            row["UploadDate"] = new DateTime(2026, 10, 21);
            row["FileLink"] = "/materials/guide.pdf";
            row["FileTypeId"] = FileTypeId;
            row["FileSize"] = isList ? 5_000_000_000L : DBNull.Value;
            row["UploadedBy"] = UploaderId;
            row["Active"] = false;
            row["CreatedAt"] = new DateTime(2026, 10, 21);
            row["UpdatedAt"] = new DateTime(2026, 10, 21);
            if (isList)
            {
                row["ErrorClassificationName"] = "Concordancia";
                row["UploadedByName"] = "Ana Pérez";
            }
            else
                foreach (var parameter in parameters.Where(p => p.Direction == ParameterDirection.Input && p.Value is not null))
                    row[parameter.Name] = parameter.Value!;
            table.Rows.Add(row);
            return Task.FromResult(ResourceResponse<DataTable>.Ok(table));
        }
    }
}
