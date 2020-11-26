using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public interface IApiClient
    {
        Task SendMessageAsync<T>(string controller, string action, T data);
    }
}