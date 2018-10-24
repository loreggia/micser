using System.Net;
using System.Net.Http;

namespace Micser.Infrastructure.Api
{
    public class ServiceResult<TData>
    {
        public ServiceResult(HttpResponseMessage message, TData data, ErrorList error)
            : this(message.StatusCode, data, error)
        {
            RequestUrl = message.RequestMessage.RequestUri.ToString();
        }

        public ServiceResult(HttpStatusCode statusCode, TData data, ErrorList error)
        {
            ResponseStatusCode = statusCode;
            Data = data;
            ErrorList = error;
        }

        public TData Data { get; set; }
        public ErrorList ErrorList { get; set; }
        public bool IsSuccess => ResponseStatusCode >= HttpStatusCode.OK && ResponseStatusCode < HttpStatusCode.BadRequest;
        public HttpStatusCode ResponseStatusCode { get; set; }
        public string RequestUrl { get; set; }

        public override string ToString()
        {
            return $"Response Status Code: {ResponseStatusCode}, URL: {RequestUrl}, Errors: {ErrorList}";
        }
    }
}