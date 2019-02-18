using Micser.App.Infrastructure.DataAccess.Models;
using Micser.Common.DataAccess;

namespace Micser.App.Infrastructure.DataAccess.Repositories
{
    public interface ISettingValueRepository : IRepository<SettingValue>
    {
        SettingValue GetByKey(string key);
    }
}