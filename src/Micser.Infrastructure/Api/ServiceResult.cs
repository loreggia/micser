using System.Linq;
using System.Net;
using System.Net.Http;

namespace Micser.Infrastructure.Api
{
    public class ServiceResult<TData>
    {
        public ServiceResult(HttpResponseMessage message, TData data, ErrorList error)
            : this(message.StatusCode, data, error)
        {
        }

        public ServiceResult(HttpStatusCode statusCode, TData data, ErrorList error)
        {
            ResponseStatusCode = statusCode;
            Data = data;
            ErrorList = error;
        }

        public TData Data { get; set; }
        public ErrorList ErrorList { get; set; }
        public bool IsSuccess => ErrorList?.Errors.Any() != true;
        public HttpStatusCode ResponseStatusCode { get; set; }
    }
}