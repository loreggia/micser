using Micser.App.Infrastructure.DataAccess.Models;
using Micser.Common;
using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.SQLite.EF6.Migrations;

namespace Micser.App.Infrastructure.DataAccess
{
    public class AppDbContext : DbContext
    {
        static AppDbContext()
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", Globals.AppDataFolder);
        }

        public AppDbContext()
            : base("DefaultConnection")
        {
        }

        // ReSharper disable once UnusedMember.Global
        public IDbSet<SettingValue> Settings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AppDbContext, ContextMigrationConfiguration>(true));
        }
    }

    public class ContextMigrationConfiguration : DbMigrationsConfiguration<AppDbContext>
    {
        public ContextMigrationConfiguration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            SetSqlGenerator("System.Data.SQLite", new SQLiteMigrationSqlGenerator());
        }
    }
}