using Serilog;
using Serilog.Events;

namespace GrammarErrorDetection.API.Logging;

/// <summary>
/// Registro mínimo: una línea por petición, los archivos creados y los errores.
/// Los logs de Microsoft/System (HttpClient, EF, etc.) solo se escriben desde Warning.
/// </summary>
public static class LogSetup
{
    private const string Template = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    public static LoggerConfiguration Configure(LoggerConfiguration configuration) => configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        // Inicio y apagado ordenado ("Application is shutting down"): distingue un cierre normal de un proceso matado desde fuera.
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .WriteTo.Console(outputTemplate: Template)
        .WriteTo.File(Path.Combine(ResolveDirectory(), "api-.log"), outputTemplate: Template,
            rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14, shared: true);

    /// <summary>LOG_DIR si existe; si no, la carpeta "logs" de Workspace (la que contiene los repos API y WEB); si no, "logs" junto al ejecutable.</summary>
    public static string ResolveDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("LOG_DIR");
        if (!string.IsNullOrWhiteSpace(configured)) return Path.GetFullPath(configured);

        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "GrammarErrorDetection.API"))
                && Directory.Exists(Path.Combine(dir.FullName, "GrammarErrorDetection.WEB")))
                return Path.Combine(dir.FullName, "logs");
        }
        return Path.Combine(AppContext.BaseDirectory, "logs");
    }
}
