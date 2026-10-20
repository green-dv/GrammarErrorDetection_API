using System.Data;
using System.Text.Json;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Registrations;
using GrammarErrorDetection.Repositories.Repositories;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

internal static class RegistrationListingChecks
{
    public static async Task Run()
    {
        var executor = new ListingExecutor();
        var repository = new RegistrationRepository(executor);
        var userId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var date = new DateOnly(2026, 10, 20);
        var rows = await repository.ListSummary(new PaginationDto<RegistrationSummaryList>
        {
            PageNumber = 2, PageSize = 10,
            Filter = new() { UserId = userId, ModuleNameId = moduleId, RegistrationDate = date, Status = "Pending" }
        });
        Check(executor.Schema == "grammar" && executor.Procedure == "Registration_List", "Llama al SP público correcto.");
        Check(executor.Parameters.Select(p => p.Name).SequenceEqual(
            ["UserId", "ModuleNameId", "RegistrationDate", "Status", "PageNumber", "PageSize", "Results"]),
            "Envía exactamente los siete parámetros del nuevo contrato.");
        Check(Equals(executor.Parameters[0].Value, userId) && Equals(executor.Parameters[1].Value, moduleId)
            && Equals(executor.Parameters[2].Value, date) && Equals(executor.Parameters[3].Value, "Pending")
            && Equals(executor.Parameters[4].Value, 2) && Equals(executor.Parameters[5].Value, 10), "Conserva filtros y paginación.");
        Check(executor.Parameters[2].Type == NpgsqlDbType.Date
            && executor.Parameters[6].Type == NpgsqlDbType.Refcursor
            && executor.Parameters[6].Direction == ParameterDirection.InputOutput, "Usa DATE y cursor INOUT.");
        Check(rows.Count == 2 && rows[0].RegistrationId == executor.FirstRegistrationId
            && rows[1].RegistrationId == executor.SecondRegistrationId
            && rows[0].UserId == executor.UserId && rows[0].ModuleId == executor.ModuleId && rows[0].ClassId == executor.ClassId
            && rows[0].FirstName == "Ana" && rows[0].LastName == "Pérez"
            && rows[0].CI == "00123" && rows[0].Phone == "70000000" && rows[0].ModuleName == "Inglés II"
            && rows[0].Status == "Pending", "Mapea los campos de estudiante, módulo y estado.");
        Check(rows[0].RegistrationDate == new DateTime(2026, 10, 20, 8, 30, 0)
            && rows[0].UpdatedAt == new DateTime(2026, 10, 20, 9, 0, 0), "Conserva fecha y hora.");
        Check(rows[1].ModuleName is null && rows[1].ModuleId is null && rows[1].ClassId is null && rows[1].Phone is null && rows[1].RegistrationDate is null,
            "Admite una inscripción sin curso, teléfono ni fecha.");
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(new Wrapper<List<RegistrationListItemDto>>
        {
            Data = rows, Count = rows.Count
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Check(json.RootElement.GetProperty("data")[0].EnumerateObject().Select(p => p.Name).ToHashSet().SetEquals(
            ["registrationId", "userId", "classId", "firstName", "lastName", "ci", "phone", "moduleName", "moduleId", "registrationDate", "status", "updatedAt"])
            && json.RootElement.GetProperty("data")[0].GetProperty("registrationId").GetGuid() == executor.FirstRegistrationId
            && json.RootElement.GetProperty("data")[0].GetProperty("userId").GetGuid() == executor.UserId
            && json.RootElement.GetProperty("data")[0].GetProperty("moduleId").GetGuid() == executor.ModuleId,
            "La API expone las doce columnas, incluido classId.");
        Check(json.RootElement.GetProperty("count").GetInt32() == 2
            && json.RootElement.GetProperty("data")[1].GetProperty("moduleName").ValueKind == JsonValueKind.Null,
            "El wrapper conserva el conteo y ModuleName=null.");
        await repository.ListSummary(new() { PageNumber = 1, PageSize = 20 });
        Check(executor.Parameters.Take(4).All(p => p.Value is null), "Los filtros omitidos se envían como null.");
        var internalRows = await repository.List(new() { PageNumber = 1, PageSize = 20 });
        Check(internalRows.Count == 2 && internalRows[0].Id == executor.FirstRegistrationId
            && internalRows[0].ClassId == executor.ClassId && internalRows[1].ClassId is null,
            "Mapea el cursor real a registros internos sin exigir las columnas de la tabla.");
        Check(internalRows.All(r => r.CreatedAt is null && r.InvoiceLink is null && r.Active is null),
            "Los valores que el SP no devuelve quedan desconocidos, sin inventar fechas ni activación.");
        using var optional = (await executor.ExecuteStoredProcedureAsync("grammar", "Registration_List", [])).GetDataOrThrow()!;
        optional.Columns.Add("CreatedAt", typeof(DateTime));
        optional.Columns.Add("InvoiceLink", typeof(string));
        optional.Columns.Add("Active", typeof(bool));
        optional.Rows[0]["CreatedAt"] = new DateTime(2026, 10, 19);
        optional.Rows[0]["InvoiceLink"] = "invoice";
        optional.Rows[0]["Active"] = false;
        var mapped = ConvertToEntity.RegistrationListToEntity(optional.Rows[0]);
        Check(mapped.Active == false && mapped.CreatedAt == new DateTime(2026, 10, 19) && mapped.InvoiceLink == "invoice",
            "Si el SP incluye las columnas opcionales, conserva sus valores reales.");
        try
        {
            await repository.List(new() { PageNumber = 1, PageSize = 20, Filter = new() { Active = true } });
            throw new Exception("No debe asumir que una inscripción está activa cuando falta Active.");
        }
        catch (DataException error) when (error.Message.Contains("Active")) { }
        Console.WriteLine("14 comprobaciones del listado de inscripciones mediante SP correctas.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class ListingExecutor : IStoredProcedureExecutor
    {
        public Guid FirstRegistrationId { get; } = Guid.NewGuid();
        public Guid SecondRegistrationId { get; } = Guid.NewGuid();
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid ModuleId { get; } = Guid.NewGuid();
        public Guid ClassId { get; } = Guid.NewGuid();
        public string? Schema { get; private set; }
        public string? Procedure { get; private set; }
        public IReadOnlyList<StoredProcedureParameter> Parameters { get; private set; } = [];
        public StoredProcedureResult? Result { get; private set; }

        public Task<ResourceResponse<DataTable>> ExecuteStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, StoredProcedureResult? result = null)
        {
            Schema = schema;
            Procedure = procedureName;
            Parameters = parameters;
            Result = result;
            var table = new DataTable();
            table.Columns.Add("RegistrationId", typeof(Guid));
            foreach (var name in new[] { "FirstName", "LastName", "CI", "Phone", "ModuleName" })
                table.Columns.Add(name, typeof(string));
            table.Columns.Add("RegistrationDate", typeof(DateTime));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("UpdatedAt", typeof(DateTime));
            table.Columns.Add("UserId", typeof(Guid));
            table.Columns.Add("ModuleId", typeof(Guid));
            table.Columns.Add("ClassId", typeof(Guid));
            table.Rows.Add(FirstRegistrationId, "Ana", "Pérez", "00123", "70000000", "Inglés II",
                new DateTime(2026, 10, 20, 8, 30, 0), "Pending", new DateTime(2026, 10, 20, 9, 0, 0),
                UserId, ModuleId, ClassId);
            table.Rows.Add(SecondRegistrationId, "Luis", "Gómez", "00456", DBNull.Value, DBNull.Value,
                DBNull.Value, "Pending", new DateTime(2026, 10, 20, 9, 0, 0),
                UserId, DBNull.Value, DBNull.Value);
            return Task.FromResult(ResourceResponse<DataTable>.Ok(table));
        }
    }

}
