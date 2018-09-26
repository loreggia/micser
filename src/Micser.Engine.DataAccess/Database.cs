using LiteDB;
using Micser.Shared.Models;
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

        public Database(string connectionString = null)
            : base(connectionString ?? ConnectionString)
        {
            var collection = GetCollection<AudioModuleDescription>();
            collection.EnsureIndex(d => d.Id, true);
            var input = new AudioModuleDescription { Id = 1, State = "{0.0.1.00000000}.{232aa400-5c3f-4f66-b5ab-afe5e2bfb594}", Type = "Micser.Engine.Audio.DeviceInputModule, Micser.Engine" };
            collection.Upsert(input);
            var output = new AudioModuleDescription { Id = 2, State = "{0.0.0.00000000}.{04097f83-4fdf-4dae-bfa4-0891f20d1352}", Type = "Micser.Engine.Audio.DeviceOutputModule, Micser.Engine" };
            collection.Upsert(output);
        }
    }
}