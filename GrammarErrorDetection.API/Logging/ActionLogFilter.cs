using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GrammarErrorDetection.API.Logging;

/// <summary>
/// Deja una línea por cada operación que modifica datos y terminó bien (POST/PUT/PATCH/DELETE), con su autor:
/// "Class.Insert por 3f2a…". Las lecturas ya quedan en el log de peticiones y los fallos los registra el middleware.
/// </summary>
public sealed class ActionLogFilter(ILogger<ActionLogFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        var method = context.HttpContext.Request.Method;
        if (executed.Exception is not null || HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method)
            || context.HttpContext.Response.StatusCode >= 400)
            return;

        var route = context.RouteData.Values;
        var user = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anónimo";
        logger.LogInformation("{Controller}.{Action} por {UserId}", route["controller"], route["action"], user);
    }
}
