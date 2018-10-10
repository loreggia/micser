using LiteDB;
using System;
using System.IO;
using Micser.Infrastructure.Models;

namespace Micser.Engine.DataAccess
{
    public class Database : LiteDatabase
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
            "Micser");

        private static readonly string ConnectionString = Path.Combine(AppDataFolder, "Database.db");

        static Database()
        {
            Directory.CreateDirectory(AppDataFolder);
        }

        public Database(string connectionString = null)
            : base(connectionString ?? ConnectionString)
        {
            Mapper.Entity<Module>().Id(x => x.Id);
            Mapper.Entity<ModuleConnection>().Id(x => x.Id);
        }
    }
}