using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.FileTypes;
using GrammarErrorDetection.Models.Grammar.SupportMaterials;
using GrammarErrorDetection.Services.Configuration;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.SupportMaterials;

public sealed partial class SupportMaterialFileService(HttpClient client, SupportMaterialFileOptions options,
    ISupportMaterialService materials, IFileTypeService fileTypes) : ISupportMaterialFileService
{
    public async Task<MaterialFileUploadResult> Store(Stream content, string fileName, long length,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (length is <= 0 or > SupportMaterialFileOptions.MaxBytes)
            throw new ValidationException("Selecciona un archivo con contenido de hasta 25 MiB.");
        if (!Path.IsPathFullyQualified(options.StoragePath))
            throw new InvalidOperationException("SupportMaterialFiles:StoragePath debe ser una ruta absoluta.");

        // El contenedor nginx solo admite nombres UUID y extensiones alfanuméricas.
        var extension = SafeExtension(fileName);
        var storedName = $"{Guid.NewGuid():N}{(extension.Length > 0 ? "." + extension : "")}";
        var path = "archivos/" + storedName;
        var uploadUri = FileUri(options.BaseUrl, path);
        var publicUri = FileUri(options.PublicBaseUrl ?? options.BaseUrl, path);
        if (publicUri.AbsoluteUri.Length > 255)
            throw new InvalidOperationException("El enlace del archivo supera los 255 caracteres.");

        Directory.CreateDirectory(options.StoragePath);
        var localPath = Path.Combine(options.StoragePath, storedName);
        var completed = false;
        try
        {
            await using var local = new FileStream(localPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous);
            var buffer = new byte[81920];
            int count;
            while ((count = await content.ReadAsync(buffer, cancellationToken)) > 0)
            {
                if (local.Length + count > SupportMaterialFileOptions.MaxBytes)
                    throw new ValidationException("El archivo supera 25 MiB.");
                await local.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
            }
            if (local.Length == 0 || local.Length != length)
                throw new ValidationException("El archivo recibido está incompleto.");
            var size = local.Length;
            local.Position = 0;
            using var message = new HttpRequestMessage(HttpMethod.Put, uploadUri);
            message.Content = new StreamContent(local);
            message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            if (!string.IsNullOrWhiteSpace(options.ApiKey)) message.Headers.Add("X-Api-Key", options.ApiKey);
            using var response = await client.SendAsync(message, cancellationToken);
            response.EnsureSuccessStatusCode();
            completed = true;
            return new(publicUri.AbsoluteUri, size);
        }
        finally
        {
            // No conservar copias parciales o rechazadas por el contenedor.
            if (!completed && File.Exists(localPath)) File.Delete(localPath);
        }
    }

    public async Task<SupportMaterialEntity> ReplaceFile(Guid materialId, Stream content, string fileName, long length,
        string? name = null, CancellationToken cancellationToken = default)
    {
        if (materialId == Guid.Empty) throw new ArgumentException("El identificador de material no puede estar vacío.", nameof(materialId));
        if (name is not null && (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 255))
            throw new ValidationException("El nombre debe tener entre 1 y 255 caracteres.");

        // Valida todo antes de subir para no dejar archivos huérfanos ante un error previsible.
        await materials.Get(materialId);
        var fileType = await FindFileType(SafeExtension(fileName));
        var stored = await Store(content, fileName, length, cancellationToken);

        return await materials.Update(new SupportMaterialUpdate
        {
            Id = materialId,
            Name = name?.Trim(),
            FileLink = stored.FileLink,
            FileSize = stored.FileSize,
            FileTypeId = fileType.Id,
            UploadDate = DateTime.UtcNow
        });
    }

    private async Task<FileTypeEntity> FindFileType(string extension)
    {
        if (extension.Length == 0)
            throw new ValidationException("El archivo debe tener una extensión reconocible (pdf, docx, png, …).");
        // FileType_List filtra con ILIKE parcial ("doc" también encuentra "docx"): se compara exacto aquí.
        var rows = await fileTypes.List(new PaginationDto<FileTypeList>
        {
            PageNumber = 1, PageSize = 50, Filter = new FileTypeList { Extension = extension, Active = true }
        });
        return rows.FirstOrDefault(row => string.Equals(row.Extension.Trim().TrimStart('.'), extension, StringComparison.OrdinalIgnoreCase))
            ?? throw new ValidationException($"El tipo de archivo .{extension} no está habilitado en el catálogo de tipos de archivo.");
    }

    private static string SafeExtension(string fileName)
    {
        var extension = Path.GetExtension((fileName ?? "").Replace('\\', '/')).TrimStart('.').ToLowerInvariant();
        return ExtensionPattern().IsMatch(extension) ? extension : "";
    }

    private static Uri FileUri(string? baseUrl, string path)
    {
        if (!Uri.TryCreate(baseUrl?.TrimEnd('/') + "/", UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("Configura una URL HTTP/HTTPS válida en SupportMaterialFiles.");
        return new Uri(uri, path);
    }

    [GeneratedRegex("^[a-z0-9]{1,10}$")]
    private static partial Regex ExtensionPattern();
}
