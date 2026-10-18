using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;

namespace GrammarErrorDetection.Infrastructure.Http
{
    public sealed class HttpApiClient : IHttpApiClient
    {
        private readonly HttpClient httpClient;
        private readonly JsonSerializerOptions jsonOptions;

        public HttpApiClient(HttpClient httpClient, JsonSerializerOptions? jsonOptions = null)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            this.httpClient = httpClient;
            this.jsonOptions = new JsonSerializerOptions(jsonOptions ?? JsonSerializerOptions.Web);
        }

        public Task<ResourceResponse<TResponse>> GetAsync<TResponse>(string requestUri)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);
            return SendAsync<TResponse>(new HttpRequestMessage(HttpMethod.Get, requestUri));
        }

        public Task<ResourceResponse<TResponse>> PostAsync<TRequest, TResponse>(string requestUri, TRequest request)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);
            var message = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = JsonContent.Create(request, options: jsonOptions)
            };
            return SendAsync<TResponse>(message);
        }

        private async Task<ResourceResponse<TResponse>> SendAsync<TResponse>(HttpRequestMessage request)
        {
            HttpStatusCode? statusCode = null;
            using (request)
            {
                try
                {
                    using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                    statusCode = response.StatusCode;
                    if (!response.IsSuccessStatusCode)
                    {
                        var message = $"La API devolvió HTTP {(int)response.StatusCode}.";
                        return ResourceResponse<TResponse>.Failure(
                            "HTTP_ERROR", message, new HttpRequestException(message, null, response.StatusCode), response.StatusCode);
                    }

                    if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.ResetContent ||
                        response.Content.Headers.ContentLength == 0)
                    {
                        return ResourceResponse<TResponse>.Ok(default, response.StatusCode);
                    }

                    var data = await response.Content.ReadFromJsonAsync<TResponse>(jsonOptions);
                    return ResourceResponse<TResponse>.Ok(data, response.StatusCode);
                }
                catch (HttpRequestException exception)
                {
                    return ResourceResponse<TResponse>.Failure("HTTP_REQUEST_FAILED", exception.Message, exception, statusCode ?? exception.StatusCode);
                }
                catch (IOException exception)
                {
                    return ResourceResponse<TResponse>.Failure("HTTP_REQUEST_FAILED", exception.Message, exception, statusCode);
                }
                catch (JsonException exception)
                {
                    return ResourceResponse<TResponse>.Failure("INVALID_JSON", "La respuesta no contiene el JSON esperado.", exception, statusCode);
                }
                catch (OperationCanceledException exception)
                {
                    return ResourceResponse<TResponse>.Failure("HTTP_TIMEOUT", "Se agotó el tiempo de espera de la petición HTTP.", exception, statusCode);
                }
            }
        }
    }
}


