using Micser.App.Infrastructure.DataAccess.Models;
using Micser.Common.DataAccess;
using System.Data.Entity;
using System.Linq;

namespace Micser.App.Infrastructure.DataAccess.Repositories
{
    public class SettingValueRepository : Repository<SettingValue>, ISettingValueRepository
    {
        public SettingValueRepository(DbContext context)
            : base(context)
        {
        }

        public SettingValue GetByKey(string key)
        {
            return DbSet.FirstOrDefault(s => s.Key == key);
        }
    }
}