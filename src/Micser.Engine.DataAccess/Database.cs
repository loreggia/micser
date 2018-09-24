using LiteDB;
using System;
using System.IO;

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

        public Database()
            : base(ConnectionString)
        {
        }
    }
}