using Microsoft.EntityFrameworkCore;
using Micser.Common;
using Micser.Common.DataAccess.Models;
using System;

namespace Micser.App.Infrastructure.DataAccess
{
    /// <summary>
    /// The entity framework database context for the app.
    /// </summary>
    public class AppDbContext : DbContext
    {
        static AppDbContext()
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", Globals.AppDataFolder);
        }

        /// <inheritdoc />
        public AppDbContext()
            : base("DefaultConnection")
        {
        }

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// The application setting store.
        /// </summary>
        public DbSet<SettingValue> Settings { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AppDbContext, ContextMigrationConfiguration>(true));
        }
    }

    /// <inheritdoc />
    public class ContextMigrationConfiguration : DbMigrationsConfiguration<AppDbContext>
    {
        /// <inheritdoc />
        public ContextMigrationConfiguration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            SetSqlGenerator("System.Data.SQLite", new SQLiteMigrationSqlGenerator());
        }
    }
}