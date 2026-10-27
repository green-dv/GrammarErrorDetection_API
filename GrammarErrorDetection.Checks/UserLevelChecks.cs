using System.ComponentModel.DataAnnotations;
using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.UserLevels;
using GrammarErrorDetection.Repositories.Repositories;
using GrammarErrorDetection.Services.Services;
using NpgsqlTypes;

internal static class UserLevelChecks
{
    public static async Task Run()
    {
        var executor = new LevelExecutor();
        var service = new UserLevelService(new UserLevelRepository(executor));
        var id = Guid.NewGuid();
        var date = new DateOnly(2026, 10, 21);
        var rows = await service.List(new()
        {
            PageNumber = 2, PageSize = 3,
            Filter = new() { Id = id, Level = 6, Name = "Maestro", MinPoints = 3_000_000_000L,
                MaxPoints = 6_000_000_000L, Active = false, CreatedAt = date }
        });
        Check(executor.Parameters.Select(p => p.Name).SequenceEqual(
            ["Id", "Level", "Name", "MinPoints", "MaxPoints", "Active", "CreatedAt", "PageNumber", "PageSize", "Results"])
            && executor.Parameters.Take(9).Select(p => p.Value).SequenceEqual(
                new object?[] { id, 6, "Maestro", 3_000_000_000L, 6_000_000_000L, false, date, 2, 3 }),
            "Los parámetros deben respetar la firma, los tipos y los filtros solicitados.");
        Check(executor.Parameters[3].Type == NpgsqlDbType.Bigint &&
            executor.Parameters[4].Type == NpgsqlDbType.Bigint &&
            executor.Parameters[6].Type == NpgsqlDbType.Date &&
            executor.Parameters[9].Direction == ParameterDirection.InputOutput,
            "Los puntos deben ser BIGINT, la fecha DATE y el cursor INOUT.");
        Check(rows.Single().MinPoints == 5_000_000_000L && rows[0].TotalCount == 6_000_000_000L &&
            !rows[0].Active, "El mapeo debe conservar BIGINT, TotalCount y false.");
        executor.Empty = true;
        Check((await service.List(new() { PageNumber = 1, PageSize = 10 })).Count == 0 &&
            executor.Parameters.Take(7).All(p => p.Value is null),
            "Sin filtros envía NULL y una página vacía devuelve una lista vacía.");
        var calls = executor.Calls;
        try
        {
            await service.List(new() { PageNumber = 1, PageSize = 10, Filter = new() { MinPoints = 11, MaxPoints = 10 } });
            throw new Exception("Debe rechazar el rango invertido.");
        }
        catch (ValidationException) { }
        Check(executor.Calls == calls, "Un rango invertido se rechaza antes de ejecutar el SP.");
        try
        {
            await service.List(new() { PageNumber = 0, PageSize = 10 });
            throw new Exception("Debe rechazar la página cero.");
        }
        catch (ArgumentOutOfRangeException) { }
        Console.WriteLine("6 comprobaciones del listado de niveles correctas.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class LevelExecutor : IStoredProcedureExecutor
    {
        public IReadOnlyList<StoredProcedureParameter> Parameters { get; private set; } = [];
        public bool Empty { get; set; }
        public int Calls { get; private set; }
        public Task<ResourceResponse<DataTable>> ExecuteStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, StoredProcedureResult? result = null)
        {
            Check(schema == "grammar" && procedureName == "UserLevel_List", "Solo debe llamar UserLevel_List.");
            Parameters = parameters;
            Calls++;
            var table = new DataTable();
            foreach (var (name, type) in new (string, Type)[]
            {
                ("Id", typeof(Guid)), ("Level", typeof(int)), ("Name", typeof(string)),
                ("MinPoints", typeof(long)), ("CreatedAt", typeof(DateTime)), ("UpdatedAt", typeof(DateTime)),
                ("Active", typeof(bool)), ("TotalCount", typeof(long))
            }) table.Columns.Add(name, type);
            if (!Empty) table.Rows.Add(Guid.NewGuid(), 6, "Maestro", 5_000_000_000L,
                new DateTime(2026, 10, 21), new DateTime(2026, 10, 21), false, 6_000_000_000L);
            return Task.FromResult(ResourceResponse<DataTable>.Ok(table));
        }
    }
}
