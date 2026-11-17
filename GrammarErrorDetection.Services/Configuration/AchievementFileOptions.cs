namespace GrammarErrorDetection.Services.Configuration;

public sealed class AchievementFileOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8081/";
    public string? PublicBaseUrl { get; set; }
    public string Directory { get; set; } = "archivos";
    public const int MaxBytes = 5 * 1024 * 1024;
}
