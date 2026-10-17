using GrammarErrorDetection.Infrastructure.Responses;

namespace GrammarErrorDetection.Infrastructure.Interfaces
{
    public interface IHttpApiClient
    {
        Task<ResourceResponse<TResponse>> GetAsync<TResponse>(string requestUri);
        Task<ResourceResponse<TResponse>> PostAsync<TRequest, TResponse>(string requestUri, TRequest request);
    }
}

