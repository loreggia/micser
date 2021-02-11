using System.Net;

namespace Micser.Infrastructure
{
#pragma warning disable RCS1194

    public class BadRequestApiException : ApiException
#pragma warning restore RCS1194
    {
        public BadRequestApiException()
            : this(null)
        {
        }

        public BadRequestApiException(string? message)
            : base(HttpStatusCode.BadRequest, "error.badRequest", message)
        {
        }
    }
}