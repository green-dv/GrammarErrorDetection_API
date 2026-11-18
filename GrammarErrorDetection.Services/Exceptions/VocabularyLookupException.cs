namespace GrammarErrorDetection.Services.Exceptions;

/// <summary>La API externa de vocabulario no pudo verificar una palabra (límite alcanzado, clave requerida, caída o respuesta inválida).</summary>
public sealed class VocabularyLookupException(string message, int? statusCode = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public int? StatusCode { get; } = statusCode;

    /// <summary>
    /// Fallo que afectará a todas las palabras (límite de cuota, clave requerida o rechazada): conviene dejar de consultar.
    /// </summary>
    public bool IsSystemic => StatusCode is 429 or 401 or 403;
}
