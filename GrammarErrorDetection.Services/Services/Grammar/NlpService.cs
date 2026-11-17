using System.Net.Http.Json;
using System.Text.Json;
using GrammarErrorDetection.Models.Nlp;
using GrammarErrorDetection.Services.Configuration;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class NlpService(HttpClient client, NlpOptions options) : INlpService
{
    public async Task<List<NlpSentenceResult>> Analyze(IReadOnlyList<NlpSentenceRequest> sentences,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("Configura NLP_API_URL con la URL HTTP/HTTPS del endpoint de análisis NLP.");
        try
        {
            using var response = await client.PostAsJsonAsync(endpoint, sentences, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                NlpValidationResponse? validation = null;
                try { validation = await response.Content.ReadFromJsonAsync<NlpValidationResponse>(cancellationToken); }
                catch (JsonException) { }
                throw new NlpApiException("La API NLP rechazó el análisis del párrafo.", (int)response.StatusCode, validation);
            }
            return await response.Content.ReadFromJsonAsync<List<NlpSentenceResult>>(cancellationToken)
                ?? throw new NlpApiException("La API NLP devolvió una respuesta vacía.");
        }
        catch (JsonException exception)
        {
            throw new NlpApiException("La API NLP devolvió un JSON incompatible con el contrato de análisis.", innerException: exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Se agotó el tiempo de espera de la API NLP.", exception);
        }
    }
}
