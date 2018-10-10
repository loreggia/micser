using LiteDB;
using Micser.Infrastructure;
using Micser.Infrastructure.Models;

namespace Micser.Engine.DataAccess
{
    public class DbContext : LiteDatabase
    {
        public DbContext(ConnectionString connectionString)
            : base(connectionString ?? Globals.DefaultConnectionString)
        {
            Mapper.Entity<Module>().Id(x => x.Id);
            Mapper.Entity<ModuleConnection>().Id(x => x.Id);
        }
    }
}