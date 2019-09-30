using Microsoft.EntityFrameworkCore;
using Micser.Common.DataAccess.Models;
using Micser.Engine.Infrastructure.DataAccess.Models;

namespace Micser.Engine.Infrastructure.DataAccess
{
    /// <summary>
    /// Migration configuration that enables automatic DB migrations.
    /// </summary>
    public class ContextMigrationConfiguration : DbMigrationsConfiguration<EngineDbContext>
    {
        /// <inheritdoc />
        public ContextMigrationConfiguration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            SetSqlGenerator("System.Data.SQLite", new SQLiteMigrationSqlGenerator());
        }
    }

    /// <summary>
    /// The EF database context for engine storage.
    /// </summary>
    public class EngineDbContext : DbContext
    {
        static EngineDbContext()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<EngineDbContext, ContextMigrationConfiguration>(true));
        }

        /// <inheritdoc />
        public EngineDbContext()
            : base("DefaultConnection")
        {
        }

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// The module connections store.
        /// </summary>
        public DbSet<ModuleConnection> ModuleConnections { get; set; }

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// The modules store.
        /// </summary>
        public DbSet<Module> Modules { get; set; }

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// The application setting store.
        /// </summary>
        public DbSet<SettingValue> Settings { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Module>()
                .HasMany(m => m.SourceModuleConnections)
                .WithRequired(c => c.SourceModule)
                .HasForeignKey(c => c.SourceModuleId);
            modelBuilder.Entity<Module>()
                .HasMany(m => m.TargetModuleConnections)
                .WithRequired(c => c.TargetModule)
                .HasForeignKey(c => c.TargetModuleId);
        }
    }
}