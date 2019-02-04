using Micser.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    public class CrudApiClient<T> : ApiClient
        where T : IIdentifiable
    {
        public CrudApiClient(string resource)
            : base(resource)
        {
        }

        public async Task<ServiceResult<T>> CreateAsync(T entity)
        {
            return await PostAsync<T>(null, entity);
        }

        public async Task<ServiceResult<T>> DeleteAsync(long id)
        {
            return await DeleteAsync<T>(null, id);
        }

        public async Task<ServiceResult<IEnumerable<T>>> GetAllAsync()
        {
            return await GetAsync<IEnumerable<T>>(null);
        }

        public async Task<ServiceResult<T>> UpdateAsync(T entity)
        {
            return await PutAsync<T>(null, entity.Id, entity);
        }
    }
}