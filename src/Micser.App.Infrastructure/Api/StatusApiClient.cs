using Micser.Common;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    public class StatusApiClient : ApiClient
    {
        public StatusApiClient()
            : base("status")
        {
        }

        public async Task<ServiceResult<ServiceStatus>> GetStatus()
        {
            return await GetAsync<ServiceStatus>(null);
        }
    }
}