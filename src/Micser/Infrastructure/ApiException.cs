using System;
using System.Net;

namespace Micser.Infrastructure
{
#pragma warning disable RCS1194

    public abstract class ApiException : Exception
#pragma warning restore RCS1194
    {
        protected ApiException(HttpStatusCode statusCode, string messageId, string? message = null)
            : base(message)
        {
            StatusCode = statusCode;
            MessageId = messageId;
        }

        public string MessageId { get; }
        public HttpStatusCode StatusCode { get; }
    }
}