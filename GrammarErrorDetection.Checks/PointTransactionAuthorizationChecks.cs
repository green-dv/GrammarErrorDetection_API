using System.Reflection;
using System.Security.Claims;
using GrammarErrorDetection.API.Controllers;
using GrammarErrorDetection.Models.Grammar.PointTransactions;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

internal static class PointTransactionAuthorizationChecks
{
    public static async Task Run()
    {
        var ownId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var service = new CapturingService();
        var controller = new PointTransactionController(service);
        SetUser("Student", ownId.ToString());
        await controller.List(new());
        Check(service.UserId == ownId, "El historial sin filtro debe limitarse al estudiante autenticado.");
        await controller.List(new() { Filter = new() { UserId = otherId } });
        Check(service.UserId == ownId, "Un estudiante no debe consultar el historial de otro usuario.");
        await controller.Total(new());
        Check(service.UserId == ownId, "El total sin filtro debe pertenecer al estudiante autenticado.");
        await controller.Total(new() { UserId = otherId });
        Check(service.UserId == ownId, "Un estudiante no debe consultar el total de otro usuario.");

        foreach (var role in new[] { "Teacher", "Coordinator" })
        {
            SetUser(role, ownId.ToString());
            await controller.List(new() { Filter = new() { UserId = otherId } });
            Check(service.UserId == otherId, "El personal puede consultar el historial del usuario solicitado.");
            await controller.Total(new() { UserId = otherId });
            Check(service.UserId == otherId, "El personal puede consultar el total del usuario solicitado.");
            await controller.Total(new());
            Check(service.UserId is null, "El personal conserva la consulta de totales sin filtro de usuario.");
        }

        foreach (var invalidId in new[] { "", "invalid", Guid.Empty.ToString() })
        {
            SetUser("Student", invalidId);
            await Reject(() => controller.List(new()));
            await Reject(() => controller.Total(new()));
        }

        Check(typeof(PointTransactionController).GetCustomAttribute<AuthorizeAttribute>() is { Roles: null },
            "La autorización global debe permitir aplicar los roles de cada operación.");
        foreach (var method in new[] { "List", "Total", "Insert", "Update" })
        {
            var roles = typeof(PointTransactionController).GetMethod(method)!
                .GetCustomAttribute<AuthorizeAttribute>()!.Roles!.Split(',');
            Check(roles.Contains("Teacher") && roles.Contains("Coordinator"), "El personal conserva el acceso.");
            Check(roles.Contains("Student") == (method is "List" or "Total"),
                "El estudiante solo tiene acceso a las consultas de puntos.");
        }
        Console.WriteLine("Comprobaciones de autorización del progreso e historial de puntos correctas.");

        void SetUser(string role, string id)
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.Role, role), new Claim(ClaimTypes.NameIdentifier, id)], "test"))
                }
            };
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static async Task Reject(Func<Task> action)
    {
        try { await action(); }
        catch (UnauthorizedAccessException) { return; }
        throw new Exception("Un token sin identidad válida debe rechazarse.");
    }

    private sealed class CapturingService : IPointTransactionService
    {
        public Guid? UserId { get; private set; }
        public Task<List<PointTransactionEntity>> List(PointTransactionListQuery request)
        {
            UserId = request.Filter?.UserId;
            return Task.FromResult(new List<PointTransactionEntity>());
        }
        public Task<PointTransactionTotalDto> Total(PointTransactionTotalQuery request)
        {
            UserId = request.UserId;
            return Task.FromResult(new PointTransactionTotalDto());
        }
        public Task<PointTransactionEntity> Insert(PointTransactionInsert request) => throw new NotSupportedException();
        public Task<PointTransactionEntity> Update(PointTransactionUpdate request) => throw new NotSupportedException();
    }
}
