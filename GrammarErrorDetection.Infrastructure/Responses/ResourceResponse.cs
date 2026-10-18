using System.Net;
using System.Runtime.ExceptionServices;

namespace GrammarErrorDetection.Infrastructure.Responses
{
    public sealed record ResourceError(string Code, string Message);

    public sealed class ResourceResponse<T>
    {
        private readonly Exception? exception;

        private ResourceResponse(T? data, ResourceError? error, HttpStatusCode? statusCode, Exception? exception)
        {
            Data = data;
            Error = error;
            StatusCode = statusCode;
            this.exception = exception;
        }

        public bool Success => Error is null;
        public T? Data { get; }
        public ResourceError? Error { get; }
        public HttpStatusCode? StatusCode { get; }

        public static ResourceResponse<T> Ok(T? data, HttpStatusCode? statusCode = null)
            => new(data, null, statusCode, null);

        public static ResourceResponse<T> Failure(string code, string message, Exception? exception = null, HttpStatusCode? statusCode = null)
            => new(default, new ResourceError(code, message), statusCode, exception);

        /// <summary>Permite a consumidores que trabajan con excepciones conservar el error original.</summary>
        public T? GetDataOrThrow()
        {
            if (Error is not null)
            {
                if (exception is not null)
                {
                    ExceptionDispatchInfo.Capture(exception).Throw();
                }

                throw new InvalidOperationException(Error.Message);
            }

            return Data;
        }
    }
}

