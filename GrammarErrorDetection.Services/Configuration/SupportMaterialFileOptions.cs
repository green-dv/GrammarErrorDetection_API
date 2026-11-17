namespace GrammarErrorDetection.Services.Configuration;

public sealed class SupportMaterialFileOptions
{
    public const long MaxBytes = 25 * 1024 * 1024;
    public required string BaseUrl { get; init; }
    public string? PublicBaseUrl { get; init; }
    public required string StoragePath { get; init; }
    public string? ApiKey { get; init; }
}
