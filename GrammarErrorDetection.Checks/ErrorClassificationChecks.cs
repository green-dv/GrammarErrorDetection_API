using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Dashboards;
using GrammarErrorDetection.Models.Grammar.ErrorClassifications;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Repositories;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services;

internal static class ErrorClassificationChecks
{
    public static async Task Run()
    {
        var executor = new CategoryExecutor();
        var matches = await new ErrorClassificationRepository(executor, executor).FindByName("Concordancia");
        Check(executor.Pages.SequenceEqual([1, 2]) && matches.Count == 1 && matches[0].Id == executor.CategoryId,
            "Recorre resultados parciales ILIKE hasta encontrar la categoría exacta y activa.");
        Check(executor.Parameters.Select(p => p.Name).SequenceEqual(
            ["Id", "Name", "Description", "Active", "CreatedAt", "PageNumber", "PageSize", "Results", "Total"])
            && Equals(executor.Parameters.Single(p => p.Name == "Name").Value, "Concordancia")
            && Equals(executor.Parameters.Single(p => p.Name == "Active").Value, true),
            "Respeta el contrato de ErrorClassification_List (dos cursores: Results y Total).");

        var listing = new CategoryExecutor();
        var page = await new ErrorClassificationRepository(listing, listing).List(new PaginationDto<ErrorClassificationList>
        {
            PageNumber = 3, PageSize = 25, Filter = new ErrorClassificationList { Name = "Concordancia", Active = true }
        });
        Check(page.TotalCount == CategoryExecutor.TotalRecords && page.Items.Count == 200
            && Equals(listing.Parameters.Single(p => p.Name == "PageNumber").Value, 3)
            && Equals(listing.Parameters.Single(p => p.Name == "PageSize").Value, 25),
            "El listado devuelve la página del primer DataTable y el total del segundo.");
        await ThrowsAsync<ArgumentOutOfRangeException>(
            () => new ErrorClassificationRepository(new CategoryExecutor(), new CategoryExecutor()).List(new PaginationDto<ErrorClassificationList> { PageNumber = 0, PageSize = 10 }),
            "Rechaza una página menor que 1 antes de llamar al SP.");

        var writes = new CategoryExecutor();
        var writer = new ErrorClassificationRepository(writes, writes);
        var created = await writer.Insert(new ErrorClassificationInsert { Name = "Preposiciones", Description = "Uso de preposiciones" });
        Check(writes.WriteProcedure == "ErrorClassification_Insert"
            && writes.Parameters.Select(p => p.Name).SequenceEqual(["Name", "Description", "Active", "ErrorClassification"])
            && Equals(writes.Parameters.Single(p => p.Name == "Active").Value, true)
            && created.Name == "Preposiciones" && created.Active,
            "Insert llama a ErrorClassification_Insert (activa por defecto) y devuelve la categoría creada.");
        var categoryToDisable = Guid.NewGuid();
        var disabled = await writer.Update(new ErrorClassificationUpdate { Id = categoryToDisable, Active = false });
        Check(writes.WriteProcedure == "ErrorClassification_Update"
            && writes.Parameters.Select(p => p.Name).SequenceEqual(["Id", "Name", "Description", "Active", "ErrorClassification"])
            && Equals(writes.Parameters.Single(p => p.Name == "Id").Value, categoryToDisable)
            && writes.Parameters.Single(p => p.Name == "Name").Value is null
            && Equals(writes.Parameters.Single(p => p.Name == "Active").Value, false)
            && disabled.Id == categoryToDisable && !disabled.Active,
            "Update con Active=false deshabilita la categoría sin tocar nombre ni descripción.");
        var calls = writes.WriteCalls;
        await ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
            () => writer.Insert(new ErrorClassificationInsert { Name = "  " }),
            "Rechaza un nombre vacío antes de llamar al SP.");
        Check(writes.WriteCalls == calls, "Una categoría inválida no llega a la base de datos.");

        var categoryId = Guid.NewGuid();
        var service = new ErrorClassificationService(new FixedRepository(categoryId));
        Check((await service.Get(categoryId)).Id == categoryId, "Get devuelve la categoría encontrada.");
        await ThrowsAsync<KeyNotFoundException>(() => service.Get(Guid.NewGuid()), "Get lanza KeyNotFound si no existe.");

        Console.WriteLine("10 comprobaciones de categorías de error correctas.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    private static async Task ThrowsAsync<TException>(Func<Task> action, string message) where TException : Exception
    {
        try { await action(); }
        catch (TException) { return; }
        throw new Exception(message);
    }

    private sealed class FixedRepository(Guid categoryId) : IErrorClassificationRepository
    {
        public Task<PagedList<ErrorClassificationEntity>> List(PaginationDto<ErrorClassificationList> request)
        {
            var id = request.Filter?.Id;
            List<ErrorClassificationEntity> items = id == categoryId ? [new() { Id = categoryId, Name = "Concordancia" }] : [];
            return Task.FromResult(new PagedList<ErrorClassificationEntity>(items, items.Count == 0 ? 0 : 42));
        }

        public Task<List<ErrorClassificationEntity>> FindByName(string name) => throw new NotSupportedException();
        public Task<ErrorClassificationEntity> Insert(ErrorClassificationInsert request) => throw new NotSupportedException();
        public Task<ErrorClassificationEntity> Update(ErrorClassificationUpdate request) => throw new NotSupportedException();
    }

    private sealed class CategoryExecutor : IStoredProcedurePageExecutor, IStoredProcedureExecutor
    {
        public string? WriteProcedure { get; private set; }
        public int WriteCalls { get; private set; }

        // Doble de escritura: devuelve la fila que construiría el SP a partir de los parámetros recibidos.
        public Task<ResourceResponse<DataTable>> ExecuteStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, StoredProcedureResult? result = null)
        {
            Check(schema == "grammar" && procedureName.StartsWith("ErrorClassification_"), "Solo debe llamar a los SP de categorías.");
            WriteProcedure = procedureName;
            WriteCalls++;
            Parameters = parameters;
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            table.Columns.Add("UpdatedAt", typeof(DateTime));
            table.Columns.Add("Active", typeof(bool));
            object? Value(string name) => parameters.SingleOrDefault(p => p.Name == name)?.Value;
            var now = DateTime.UtcNow;
            table.Rows.Add(Value("Id") ?? Guid.NewGuid(), Value("Name") ?? "Existente", Value("Description"), now, now, Value("Active") ?? true);
            return Task.FromResult(ResourceResponse<DataTable>.Ok(table));
        }

        public const long TotalRecords = 202;
        public Guid CategoryId { get; } = Guid.NewGuid();
        public List<int> Pages { get; } = [];
        public IReadOnlyList<StoredProcedureParameter> Parameters { get; private set; } = [];

        public Task<ResourceResponse<StoredProcedurePage>> ExecutePagedStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, string rowsCursorParameter = "Results",
            string totalCursorParameter = "Total")
        {
            Check(schema == "grammar" && procedureName == "ErrorClassification_List", "Solo debe llamar al SP de categorías.");
            Check(rowsCursorParameter == "Results" && totalCursorParameter == "Total", "Usa los cursores Results y Total.");
            Parameters = parameters;
            var number = (int)parameters.Single(p => p.Name == "PageNumber").Value!;
            Pages.Add(number);
            var rows = new DataTable();
            rows.Columns.Add("Id", typeof(Guid));
            rows.Columns.Add("Name", typeof(string));
            rows.Columns.Add("Description", typeof(string));
            rows.Columns.Add("CreatedAt", typeof(DateTime));
            rows.Columns.Add("UpdatedAt", typeof(DateTime));
            rows.Columns.Add("Active", typeof(bool));
            var now = DateTime.UtcNow;
            if (number != 2)
                for (var index = 0; index < 200; index++) rows.Rows.Add(Guid.NewGuid(), "Concordancia extendida", null, now, now, true);
            else
            {
                rows.Rows.Add(CategoryId, "CONCORDANCIA", null, now, now, true);
                rows.Rows.Add(Guid.NewGuid(), "Concordancia", null, now, now, false);
            }
            var total = new DataTable();
            total.Columns.Add("TotalCount", typeof(long));
            total.Rows.Add(TotalRecords);
            return Task.FromResult(ResourceResponse<StoredProcedurePage>.Ok(new StoredProcedurePage(rows, total)));
        }
    }
}
