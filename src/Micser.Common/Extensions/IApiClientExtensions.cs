using Micser.Common.Api;
using System.Threading;
using System.Threading.Tasks;

namespace Micser.Common.Extensions
{
    public static class IApiClientExtensions
    {
        public static Task<bool> ConnectAsync(this IApiClient apiClient)
        {
            return apiClient.ConnectAsync(CancellationToken.None);
        }

        public static Task<ApiResponse> SendMessageAsync(this IApiClient apiClient, ApiRequest request)
        {
            return apiClient.SendMessageAsync(request, CancellationToken.None);
        }

        public static Task<ApiResponse> SendMessageAsync(this IApiClient apiClient, string resourceName, string action = null, object content = null)
        {
            return apiClient.SendMessageAsync(new ApiRequest(resourceName, action, content), CancellationToken.None);
        }
    }
}