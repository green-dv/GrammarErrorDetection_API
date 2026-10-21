using System.Net;
using System.Text;
using System.Text.Json;
using GrammarErrorDetection.Models.Nlp;
using GrammarErrorDetection.Services.Configuration;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Services.Services;

internal static class NlpServiceChecks
{
    public static async Task Run()
    {
        var count = 0;
        var handler = new Handler();
        using var client = new HttpClient(handler);
        var service = new NlpService(client, new NlpOptions { Endpoint = "https://localhost:8443/api/analyze" });
        var result = await service.Analyze([new NlpSentenceRequest { SentenceId = "sentence-1", Text = "She go." }]);
        Check(handler.Method == HttpMethod.Post && handler.Url == "https://localhost:8443/api/analyze", "Invoca el endpoint NLP correcto por POST.");
        using var request = JsonDocument.Parse(handler.Body!);
        Check(request.RootElement.ValueKind == JsonValueKind.Array
            && request.RootElement[0].GetProperty("sentence_id").GetString() == "sentence-1"
            && request.RootElement[0].GetProperty("text").GetString() == "She go.", "Envía un array con los nombres snake_case requeridos.");
        Check(result[0].SentenceId == "sentence-1" && result[0].Errors[0].ClassificationName == "Concordancia"
            && result[0].Errors[0].SuggestedCorrection == "goes" && result[0].Errors[0].ConfidenceScore == 0.95m,
            "Deserializa la respuesta real de análisis.");
        handler.Status = HttpStatusCode.UnprocessableEntity;
        handler.Response = """{"detail":[{"loc":["body",0,"text"],"msg":"field required","type":"missing"}]}""";
        try { await service.Analyze([]); throw new Exception("Se ignoró el error 422."); }
        catch (NlpApiException exception)
        {
            using var details = JsonDocument.Parse(exception.Details);
            Check(details.RootElement.GetProperty("upstreamStatusCode").GetInt32() == 422
                && details.RootElement.GetProperty("detail")[0].GetProperty("loc")[1].GetInt32() == 0
                && details.RootElement.GetProperty("detail")[0].GetProperty("msg").GetString() == "field required",
                "Conserva el detalle de validación NLP, incluida loc con strings e índices.");
        }
        handler.Status = HttpStatusCode.OK;
        handler.Response = """{"detail":[]}""";
        try { await service.Analyze([]); throw new Exception("Se aceptó un objeto en lugar de una lista."); }
        catch (NlpApiException) { count++; }
        handler.Response = """[{"sentence_id":"sentence-1"}]""";
        try { await service.Analyze([]); throw new Exception("Se aceptó una respuesta sin errors."); }
        catch (NlpApiException) { count++; }
        Console.WriteLine($"{count} comprobaciones del cliente NLP correctas.");

        void Check(bool condition, string message) { if (!condition) throw new Exception(message); count++; }
    }

    private sealed class Handler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public string Response { get; set; } = """[{"sentence_id":"sentence-1","errors":[{"error_token_start_index":4,"error_token_end_index":6,"suggested_correction":"goes","confidence_score":0.95,"error_classification_name":"Concordancia"}]}]""";
        public string? Body { get; private set; }
        public string? Url { get; private set; }
        public HttpMethod? Method { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Url = request.RequestUri!.ToString();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(Status) { Content = new StringContent(Response, Encoding.UTF8, "application/json") };
        }
    }
}
