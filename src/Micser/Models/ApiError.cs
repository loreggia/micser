using System.Net;

namespace Micser.Models
{
    public class ApiError
    {
        public ApiError(HttpStatusCode statusCode, string messageId, string message)
        {
            StatusCode = statusCode;
            MessageId = messageId;
            Message = message;
        }

        public string Message { get; }
        public string MessageId { get; }
        public HttpStatusCode StatusCode { get; }
    }
}