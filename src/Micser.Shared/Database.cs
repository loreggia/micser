using System;
using System.IO;
using System.Linq;
using LiteDB;
using Micser.Shared.Models;

namespace Micser.Shared
{
    public class Database : LiteDatabase
    {
        private static readonly string ConnectionString = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
            "Micser",
            "Database.db");

        public Database()
            : base(ConnectionString)
        {
        }

        public IQueryable<AudioModuleDescription> AudioModuleDescriptions => GetCollection<AudioModuleDescription>().;
    }
}
