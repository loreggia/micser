using LiteDB;
using Micser.Infrastructure.Models;
using System.IO;

namespace Micser.Engine.DataAccess
{
    public class Database : LiteDatabase
    {
        static Database()
        {
            Directory.CreateDirectory(AppDataFolder);
        }

        public Database(ConnectionString connectionString)
            : base(connectionString ?? ConnectionString)
        {
            Mapper.Entity<Module>().Id(x => x.Id);
            Mapper.Entity<ModuleConnection>().Id(x => x.Id);
        }
    }
}