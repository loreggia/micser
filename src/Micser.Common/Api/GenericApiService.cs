using System.Threading.Tasks;
using Grpc.Core;

namespace Micser.Common.Api
{
    public class GenericApiService : Generic.GenericBase
    {
        public override Task<Response> SendMessage(Request request, ServerCallContext context)
        {
            return base.SendMessage(request, context);
        }
    }
}