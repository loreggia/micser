using System.Net;

namespace Micser.UI.Infrastructure
{
#pragma warning disable RCS1194

    public class BadRequestApiException : ApiException
#pragma warning restore RCS1194
    {
        public BadRequestApiException()
            : this("Bad request")
        {
        }

        public BadRequestApiException(string message)
            : base(HttpStatusCode.BadRequest, message)
        {
        }
    }
}