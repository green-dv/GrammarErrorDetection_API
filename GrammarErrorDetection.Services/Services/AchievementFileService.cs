using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Services.Configuration;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class AchievementFileService(HttpClient client, AchievementFileOptions options) : IAchievementFileService
{
    public async Task<AchievementIconResult> Upload(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > AchievementFileOptions.MaxBytes)
                throw new ValidationException("La imagen no puede superar 5 MiB.");
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }
        var bytes = buffer.ToArray();
        var (extension, mediaType) = DetectImage(bytes);
        var folder = options.Directory.Trim('/');
        if (string.IsNullOrWhiteSpace(folder) || folder.Split('/').Any(p => p is "." or ".." || p.Contains('\\')))
            throw new InvalidOperationException("La carpeta del servidor de archivos no es válida.");
        var path = string.Join('/', folder.Split('/').Select(Uri.EscapeDataString)) + $"/{Guid.NewGuid():N}.{extension}";
        var uploadUri = FileUri(options.BaseUrl, path);
        var publicUri = FileUri(options.PublicBaseUrl ?? options.BaseUrl, path);
        if (publicUri.AbsoluteUri.Length > 255)
            throw new InvalidOperationException("La URL de la imagen supera los 255 caracteres permitidos por IconLink.");

        using var request = new HttpRequestMessage(HttpMethod.Put, uploadUri);
        request.Content = new ByteArrayContent(bytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"El servidor de archivos rechazó PUT {uploadUri} con HTTP {(int)response.StatusCode} ({response.ReasonPhrase}). "
                + "Revisa AchievementFiles:BaseUrl y AchievementFiles:Directory; el servidor Nginx actual publica /archivos/<uuid>.ext.",
                inner: null, statusCode: response.StatusCode);
        return new(publicUri.AbsoluteUri);
    }

    private static Uri FileUri(string baseUrl, string path)
    {
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("La URL del servidor de archivos no es válida.");
        return new Uri(uri, path);
    }

    private static (string Extension, string MediaType) DetectImage(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return ("png", "image/png");
        if (bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255)
            return ("jpg", "image/jpeg");
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
            return ("webp", "image/webp");
        throw new ValidationException("Envía una imagen JPEG, PNG o WebP.");
    }
}
