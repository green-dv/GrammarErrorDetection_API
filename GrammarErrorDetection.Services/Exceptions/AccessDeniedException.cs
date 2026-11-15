namespace GrammarErrorDetection.Services.Exceptions;

/// <summary>El usuario autenticado no puede realizar la operación sobre ese recurso (HTTP 403).</summary>
public sealed class AccessDeniedException(string message) : Exception(message);
