using System.Net.Http;

namespace Micser.Infrastructure.Api
{
    public class ServiceResult
    {
        public ServiceResult(int statusCode, string statusText)
        {
            StatusCode = statusCode;
            StatusText = statusText;
        }

        public ServiceResult(HttpResponseMessage message)
            : this((int)message.StatusCode, message.ReasonPhrase)
        {
        }

        public bool IsSuccess => StatusCode >= 200 && StatusCode <= 299;
        public int StatusCode { get; }
        public string StatusText { get; }
    }

    public class ServiceResult<TData, TError> : ServiceResult
    {
        public ServiceResult(HttpResponseMessage message, TData data, TError error)
            : this((int)message.StatusCode, message.ReasonPhrase, data, error)
        {
        }

        public ServiceResult(int statusCode, string statusText, TData data, TError error)
            : base(statusCode, statusText)
        {
            Data = data;
            Error = error;
        }

        public TData Data { get; }
        public TError Error { get; }
    }
}