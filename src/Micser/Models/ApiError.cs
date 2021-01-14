using System.Net;

namespace Micser.Models
{
    public class ApiError
    {
        public ApiError(HttpStatusCode statusCode, string message)
        {
            StatusCode = statusCode;
            Message = message;
        }

        public string Message { get; }
        public HttpStatusCode StatusCode { get; }
    }
}