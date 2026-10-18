namespace GrammarErrorDetection.Services.Exceptions;

public sealed class AuthConflictException(string message) : Exception(message);
