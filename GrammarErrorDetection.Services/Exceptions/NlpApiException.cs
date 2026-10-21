using System.Text.Json;
using GrammarErrorDetection.Models.Nlp;

namespace GrammarErrorDetection.Services.Exceptions;

public sealed class NlpApiException : Exception
{
    public string Details { get; }

    public NlpApiException(string message, int? upstreamStatusCode = null,
        NlpValidationResponse? validation = null, Exception? innerException = null) : base(message, innerException)
    {
        Details = JsonSerializer.Serialize(new { upstreamStatusCode, detail = validation?.Detail });
    }
}
