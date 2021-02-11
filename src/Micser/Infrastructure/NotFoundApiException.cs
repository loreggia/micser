using System.Net;

namespace Micser.Infrastructure
{
#pragma warning disable RCS1194

    public class NotFoundApiException : ApiException
#pragma warning restore RCS1194
    {
        public NotFoundApiException()
            : this(null)
        {
        }

        public NotFoundApiException(string? message)
            : base(HttpStatusCode.NotFound, "error.notFound", message)
        {
        }
    }
}