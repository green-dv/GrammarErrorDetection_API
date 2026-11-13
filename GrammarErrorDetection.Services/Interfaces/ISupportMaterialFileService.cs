using GrammarErrorDetection.Models.Grammar.SupportMaterials;

namespace GrammarErrorDetection.Services.Interfaces;

public interface ISupportMaterialFileService
{
    /// <summary>Guarda el archivo en el servidor de archivos y devuelve su enlace público y tamaño.</summary>
    Task<MaterialFileUploadResult> Store(Stream content, string fileName, long length, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sube un archivo y lo asigna a un material existente en un solo paso: actualiza enlace, tamaño,
    /// tipo de archivo (según la extensión) y fecha de subida. El nombre es opcional.
    /// </summary>
    Task<SupportMaterialEntity> ReplaceFile(Guid materialId, Stream content, string fileName, long length,
        string? name = null, CancellationToken cancellationToken = default);
}
