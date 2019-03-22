using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    public class StatusApiClient : ApiClient
    {
        public async Task<ServiceResult<bool>> GetStatus()
        {
            return null;
        }
    }
}