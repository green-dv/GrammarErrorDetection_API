using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using GrammarErrorDetection.Repositories.Repositories;

internal static class ErrorClassificationChecks
{
    public static async Task Run()
    {
        var executor = new CategoryExecutor();
        var matches = await new ErrorClassificationRepository(executor).FindByName("Concordancia");
        Check(executor.Pages.SequenceEqual([1, 2]) && matches.Count == 1 && matches[0].Id == executor.CategoryId,
            "Recorre resultados parciales ILIKE hasta encontrar la categoría exacta y activa.");
        Check(executor.Parameters.Select(p => p.Name).SequenceEqual(
            ["Id", "Name", "Description", "Active", "CreatedAt", "PageNumber", "PageSize", "Results"])
            && Equals(executor.Parameters.Single(p => p.Name == "Name").Value, "Concordancia")
            && Equals(executor.Parameters.Single(p => p.Name == "Active").Value, true),
            "Respeta el contrato de ErrorClassification_List.");
        Console.WriteLine("2 comprobaciones de resolución de categorías correctas.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class CategoryExecutor : IStoredProcedureExecutor
    {
        public Guid CategoryId { get; } = Guid.NewGuid();
        public List<int> Pages { get; } = [];
        public IReadOnlyList<StoredProcedureParameter> Parameters { get; private set; } = [];
        public Task<ResourceResponse<DataTable>> ExecuteStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, StoredProcedureResult? result = null)
        {
            Check(schema == "grammar" && procedureName == "ErrorClassification_List", "Solo debe llamar al SP de categorías.");
            Parameters = parameters;
            var page = (int)parameters.Single(p => p.Name == "PageNumber").Value!;
            Pages.Add(page);
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Active", typeof(bool));
            if (page == 1)
                for (var index = 0; index < 200; index++) table.Rows.Add(Guid.NewGuid(), "Concordancia extendida", true);
            else
            {
                table.Rows.Add(CategoryId, "CONCORDANCIA", true);
                table.Rows.Add(Guid.NewGuid(), "Concordancia", false);
            }
            return Task.FromResult(ResourceResponse<DataTable>.Ok(table));
        }
    }
}
