using System.Net;

namespace Micser.UI.Infrastructure
{
#pragma warning disable RCS1194

    public class NotFoundApiException : ApiException
#pragma warning restore RCS1194
    {
        public NotFoundApiException()
            : this("Not found")
        {
        }

        public NotFoundApiException(string message)
            : base(HttpStatusCode.NotFound, message)
        {
        }
    }
}