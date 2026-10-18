using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using GrammarErrorDetection.Models.APIRequest;
using Microsoft.AspNetCore.Mvc;
using GrammarErrorDetection.Services.Exceptions;

namespace GrammarErrorDetection.API.Middleware
{
    public sealed class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlerMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);

                // Completa errores HTTP sin cuerpo, como una ruta inexistente o un acceso denegado.
                if (context.Response.StatusCode >= 400 &&
                    !context.Response.HasStarted &&
                    string.IsNullOrEmpty(context.Response.ContentType) &&
                    context.Response.ContentLength is null or 0)
                {
                    await WriteError(context, context.Response.StatusCode, GetStatusMessage(context.Response.StatusCode));
                }
            }
            catch (Exception exception)
            {
                var (statusCode, message) = GetExceptionResponse(exception);
                _logger.LogError(
                    exception,
                    "Error al procesar {Method} {Path}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);

                if (context.Response.HasStarted)
                {
                    throw;
                }

                context.Response.Clear();
                var details = _environment.IsDevelopment() ? exception.ToString() : string.Empty;
                await WriteError(context, statusCode, message, details);
            }
        }

        public static IActionResult CreateValidationErrorResponse(ActionContext context)
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error =>
                    $"{entry.Key}: {(string.IsNullOrWhiteSpace(error.ErrorMessage) ? "El valor proporcionado no es válido." : error.ErrorMessage)}"));

            return new BadRequestObjectResult(CreateErrorResponse(
                StatusCodes.Status400BadRequest,
                GetStatusMessage(StatusCodes.Status400BadRequest),
                string.Join(" | ", errors)));
        }

        private static Task WriteError(HttpContext context, int statusCode, string message, string details = "")
        {
            context.Response.StatusCode = statusCode;
            context.Response.Headers.CacheControl = "no-store";
            return context.Response.WriteAsJsonAsync(CreateErrorResponse(statusCode, message, details));
        }

        private static Wrapper<object> CreateErrorResponse(int statusCode, string message, string details)
        {
            return new Wrapper<object>
            {
                Error = new APIError
                {
                    ErrorCode = statusCode,
                    ErrorMessage = message,
                    ErrorDetails = details
                }
            };
        }

        private static (int StatusCode, string Message) GetExceptionResponse(Exception exception)
        {
            return exception switch
            {
                BadHttpRequestException request => (request.StatusCode, GetStatusMessage(request.StatusCode)),
                ValidationException or ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
                KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
                AuthConflictException => (StatusCodes.Status409Conflict, exception.Message),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Credenciales o código de verificación no válidos."),
                DbException { SqlState: "23505" or "23503" } => (StatusCodes.Status409Conflict, GetStatusMessage(StatusCodes.Status409Conflict)),
                DbException { SqlState: "23502" or "23514" or "22001" or "22P02" } => (StatusCodes.Status400BadRequest, GetStatusMessage(StatusCodes.Status400BadRequest)),
                HttpRequestException => (StatusCodes.Status502BadGateway, GetStatusMessage(StatusCodes.Status502BadGateway)),
                TimeoutException or OperationCanceledException => (StatusCodes.Status504GatewayTimeout, GetStatusMessage(StatusCodes.Status504GatewayTimeout)),
                _ => (StatusCodes.Status500InternalServerError, GetStatusMessage(StatusCodes.Status500InternalServerError))
            };
        }

        private static string GetStatusMessage(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => "Los datos enviados no son válidos.",
                StatusCodes.Status401Unauthorized => "Debes autenticarte para acceder al recurso.",
                StatusCodes.Status403Forbidden => "No tienes permiso para realizar esta operación.",
                StatusCodes.Status404NotFound => "No se encontró el recurso solicitado.",
                StatusCodes.Status405MethodNotAllowed => "El método HTTP no está permitido.",
                StatusCodes.Status409Conflict => "La operación entra en conflicto con un registro existente.",
                StatusCodes.Status413PayloadTooLarge => "El contenido de la solicitud es demasiado grande.",
                StatusCodes.Status415UnsupportedMediaType => "El tipo de contenido no es compatible.",
                StatusCodes.Status422UnprocessableEntity => "No se pudo procesar la solicitud.",
                StatusCodes.Status429TooManyRequests => "Se han realizado demasiadas solicitudes.",
                StatusCodes.Status502BadGateway => "No se pudo completar la solicitud al servicio externo.",
                StatusCodes.Status503ServiceUnavailable => "El servicio no está disponible temporalmente.",
                StatusCodes.Status504GatewayTimeout => "Se agotó el tiempo de espera de la operación.",
                >= 500 => "Ocurrió un error interno en el servidor.",
                _ => "No se pudo completar la solicitud."
            };
        }
    }
}
