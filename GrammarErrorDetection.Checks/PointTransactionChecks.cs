using System.ComponentModel.DataAnnotations;
using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using GrammarErrorDetection.Models.Grammar.PointTransactions;
using GrammarErrorDetection.Repositories.Repositories;
using GrammarErrorDetection.Services.Services;

internal static class PointTransactionChecks
{
    public static async Task Run()
    {
        var executor = new PointExecutor();
        var service = new PointTransactionService(new PointTransactionRepository(executor));
        var registrationId = Guid.NewGuid();
        var inserted = await service.Insert(new() { RegistrationId = registrationId, Amount = 10, Reason = "Participación" });
        Check(executor.Procedure == "PointTransaction_Insert" &&
            executor.Parameters.All(p => p.Name != "GrantedAt"), "La fecha omitida debe usar el DEFAULT del SP.");
        Check(Equals(executor.Value("RegistrationId"), registrationId) &&
            inserted.RegistrationId == registrationId && inserted.Amount == 10, "Los puntos pertenecen a la inscripción elegida.");
        Check(Equals(executor.Value("Active"), true), "Las asignaciones nuevas deben estar activas.");
        await service.Insert(new() { RegistrationId = registrationId, Amount = -5, Reason = "Ajuste",
            GrantedAt = new DateTime(2026, 10, 21, 14, 0, 0, DateTimeKind.Utc), Active = false });
        Check(Equals(executor.Value("GrantedAt"), new DateTime(2026, 10, 21, 10, 0, 0)) &&
            Equals(executor.Value("Amount"), -5), "Conserva ajustes negativos y convierte la fecha a Bolivia.");
        await service.Update(new() { Id = inserted.Id });
        Check(executor.Procedure == "PointTransaction_Update" && executor.Parameters
            .Where(p => p.Name != "Id").All(p => p.Value is null), "Actualizar solo ID conserva los demás campos.");
        await service.Update(new() { Id = inserted.Id, Amount = 0, Active = false });
        Check(Equals(executor.Value("Amount"), 0) && Equals(executor.Value("Active"), false),
            "Cero y false deben enviarse sin confundirse con NULL.");
        var userId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var total = await service.Total(new() { UserId = userId, ClassId = classId });
        Check(total.TotalPoints == 5_000_000_000L && Equals(executor.Value("UserId"), userId) &&
            Equals(executor.Value("ClassId"), classId), "El total soporta BIGINT y ambos filtros.");
        await service.Total(new());
        Check(executor.Value("ClassId") is null && executor.Value("UserId") is null,
            "Los filtros omitidos del total se envían como NULL.");
        var history = await service.List(new());
        Check(history.Count == 1 && Equals(executor.Value("PageNumber"), 1) &&
            Equals(executor.Value("PageSize"), 10), "El historial usa página 1 y tamaño 10 por defecto.");
        Check(executor.Parameters.Where(p => p.Name is not ("PageNumber" or "PageSize" or "Results"))
            .All(p => p.Value is null), "El historial sin filtros no restringe los movimientos.");
        var day = new DateOnly(2026, 10, 21);
        history = await service.List(new()
        {
            PageNumber = 2, PageSize = 5,
            Filter = new() { Id = inserted.Id, RegistrationId = registrationId, UserId = userId, Amount = 0,
                Reason = "Ajuste", GrantedAt = day, CreatedAt = day, Active = false }
        });
        Check(executor.Procedure == "PointTransaction_List" &&
            Equals(executor.Value("UserId"), userId) &&
            executor.Parameters.Single(p => p.Name == "UserId").Type == NpgsqlTypes.NpgsqlDbType.Uuid &&
            Equals(executor.Value("Id"), inserted.Id) && Equals(executor.Value("RegistrationId"), registrationId) &&
            Equals(executor.Value("GrantedAt"), day) && Equals(executor.Value("CreatedAt"), day) &&
            executor.Parameters.Single(p => p.Name == "GrantedAt").Type == NpgsqlTypes.NpgsqlDbType.Date &&
            Equals(executor.Value("PageNumber"), 2) && Equals(executor.Value("PageSize"), 5),
            "El historial envía los filtros de fecha y paginación al procedimiento.");
        Check(history.Single().Amount == 0 && !history.Single().Active && history.Single().Reason == "Ajuste",
            "El historial conserva cero y false al mapear los resultados.");
        await Reject(() => service.List(new() { Filter = new() { Id = Guid.Empty } }));
        await Reject(() => service.List(new() { Filter = new() { UserId = Guid.Empty } }));
        await Reject(() => service.List(new() { Filter = new() { Reason = new string('x', 256) } }));
        try { await service.List(new() { PageNumber = 0 }); throw new Exception("Debe rechazar página cero."); }
        catch (ArgumentOutOfRangeException) { }
        try { await service.List(new() { PageSize = 0 }); throw new Exception("Debe rechazar tamaño cero."); }
        catch (ArgumentOutOfRangeException) { }
        executor.Missing = true;
        Check((await service.List(new())).Count == 0, "Un historial vacío devuelve una lista vacía.");
        executor.Missing = false;
        await Reject(() => service.Insert(new() { RegistrationId = registrationId, Reason = "Sin importe" }));
        await Reject(() => service.Insert(new() { Amount = 10, Reason = "Sin inscripción" }));
        await Reject(() => service.Insert(new() { RegistrationId = registrationId, Amount = 10, Reason = " " }));
        await Reject(() => service.Update(new() { Id = inserted.Id, Reason = new string('x', 256) }));
        executor.Missing = true;
        try { await service.Update(new() { Id = inserted.Id }); throw new Exception("Debe devolver no encontrado."); }
        catch (KeyNotFoundException) { }
        Console.WriteLine("Comprobaciones de asignación e historial de puntos correctas.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static async Task Reject(Func<Task> action)
    {
        try { await action(); }
        catch (ValidationException) { return; }
        throw new Exception("Debe rechazar la entrada inválida.");
    }

    private sealed class PointExecutor : IStoredProcedureExecutor
    {
        public string Procedure { get; private set; } = "";
        public IReadOnlyList<StoredProcedureParameter> Parameters { get; private set; } = [];
        public bool Missing { get; set; }
        public object? Value(string name) => Parameters.Single(p => p.Name == name).Value;

        public Task<ResourceResponse<DataTable>> ExecuteStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, StoredProcedureResult? result = null)
        {
            Check(schema == "grammar", "Debe ejecutar el SP en grammar.");
            Procedure = procedureName;
            Parameters = parameters;
            var table = new DataTable();
            if (procedureName == "PointTransaction_Total")
            {
                Check(parameters.Last().Direction == ParameterDirection.InputOutput, "El total debe usar cursor INOUT.");
                table.Columns.Add("TotalPoints", typeof(long));
                table.Rows.Add(5_000_000_000L);
            }
            else
            {
                if (procedureName == "PointTransaction_List")
                    Check(parameters.Single(p => p.Name == "Results").Direction == ParameterDirection.InputOutput &&
                        Equals(Value("Results"), "pointtransaction_list") && result is not null,
                        "El historial debe usar cursor INOUT.");
                else
                    Check(parameters.Single(p => p.Name == "PointTransaction").Direction == ParameterDirection.Output,
                        "La escritura debe usar la salida composite.");
                foreach (var (name, type) in new (string, Type)[]
                {
                    ("Id", typeof(Guid)), ("RegistrationId", typeof(Guid)), ("Amount", typeof(int)),
                    ("Reason", typeof(string)), ("GrantedAt", typeof(DateTime)), ("CreatedAt", typeof(DateTime)),
                    ("UpdatedAt", typeof(DateTime)), ("Active", typeof(bool))
                }) table.Columns.Add(name, type);
                if (!Missing) table.Rows.Add(Guid.NewGuid(), Value("RegistrationId") ?? Guid.NewGuid(),
                    Value("Amount") ?? 0, Value("Reason") ?? "Ajuste", new DateTime(2026, 10, 21),
                    new DateTime(2026, 10, 21), new DateTime(2026, 10, 21), Value("Active") ?? true);
            }
            return Task.FromResult(ResourceResponse<DataTable>.Ok(table));
        }
    }
}
