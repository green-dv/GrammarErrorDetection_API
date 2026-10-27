using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using GrammarErrorDetection.API.Controllers;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Repositories.Repositories;
using GrammarErrorDetection.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NpgsqlTypes;

internal static class UserLeaderboardChecks
{
    public static async Task Run()
    {
        var executor = new LeaderboardExecutor();
        var service = new UserLeaderboardService(new UserLeaderboardRepository(executor));
        var userId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var controller = new UserLeaderboardController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, "Student")], "test"))
                }
            }
        };
        var result = await controller.List(new() { UserId = userId, Bimester = 6, Year = 2026, ModuleNameId = moduleId });
        Check(Equals(executor.Value("UserId"), userId) && Equals(executor.Value("Bimester"), 6) &&
            Equals(executor.Value("Year"), 2026) && Equals(executor.Value("ModuleNameId"), moduleId),
            "Debe enviar el UserId del body y los filtros solicitados al SP.");
        Check(result.Count == 2 && result.Data![0].TotalPoints == 5_000_000_000L && result.Data[0].Position == 1 &&
            !result.Data[0].IsCurrentUser && result.Data[0].IsTop10,
            "Debe conservar otros usuarios del ranking y valores BIGINT.");
        var current = result.Data![1];
        Check(current.UserId == userId && current.Position is null && current.ModuleNameId is null &&
            current.ModuleName is null && current.TotalPoints == 0 && current.IsCurrentUser && !current.IsTop10,
            "Debe aceptar al usuario actual sin posición ni módulo.");
        await controller.List(new() { UserId = userId });
        Check(executor.Value("Bimester") is null && executor.Value("Year") is null &&
            executor.Value("ModuleNameId") is null, "Omitir filtros debe consultar el ranking global.");
        foreach (var query in new UserLeaderboardQuery[]
        {
            new() { UserId = userId, Bimester = 0 }, new() { UserId = userId, Bimester = 7 },
            new() { UserId = userId, Year = 0 }, new() { UserId = userId, ModuleNameId = Guid.Empty }
        })
        {
            try { await service.List(userId, query); throw new Exception("Debe rechazar filtros inválidos."); }
            catch (ValidationException) { }
        }
        try { await controller.List(new()); throw new Exception("Debe rechazar UserId omitido o vacío."); }
        catch (ValidationException) { }
        Console.WriteLine("Comprobaciones del leaderboard correctas.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class LeaderboardExecutor : IStoredProcedureExecutor
    {
        private IReadOnlyList<StoredProcedureParameter> parameters = [];
        public object? Value(string name) => parameters.Single(p => p.Name == name).Value;
        public Task<ResourceResponse<DataTable>> ExecuteStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, StoredProcedureResult? result = null)
        {
            this.parameters = parameters;
            Check(schema == "grammar" && procedureName == "User_Leaderboard", "Debe ejecutar User_Leaderboard.");
            var cursor = parameters.Single(p => p.Name == "Results");
            Check(cursor.Type == NpgsqlDbType.Refcursor && cursor.Direction == ParameterDirection.InputOutput &&
                Equals(cursor.Value, "user_leaderboard") && result is not null, "Debe usar el cursor INOUT.");
            var table = new DataTable();
            foreach (var (name, type) in new (string, Type)[]
            {
                ("Position", typeof(long)), ("UserId", typeof(Guid)), ("FirstName", typeof(string)),
                ("LastName", typeof(string)), ("UserName", typeof(string)), ("ModuleNameId", typeof(Guid)),
                ("ModuleName", typeof(string)), ("TotalPoints", typeof(long)),
                ("IsCurrentUser", typeof(bool)), ("IsTop10", typeof(bool))
            }) table.Columns.Add(name, type);
            table.Rows.Add(1L, Guid.NewGuid(), "Ana", "Pérez", "Ana Pérez", Guid.NewGuid(), "Módulo I",
                5_000_000_000L, false, true);
            table.Rows.Add(DBNull.Value, Value("UserId"), "Luis", "Rojas", "Luis Rojas", DBNull.Value,
                DBNull.Value, 0L, true, false);
            return Task.FromResult(ResourceResponse<DataTable>.Ok(table));
        }
    }
}
