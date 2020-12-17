using System;
using System.Net;

namespace Micser.UI.Infrastructure
{
#pragma warning disable RCS1194

    public abstract class ApiException : Exception
#pragma warning restore RCS1194
    {
        protected ApiException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}