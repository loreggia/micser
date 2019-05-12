using Micser.Engine.Infrastructure.DataAccess.Models;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.SQLite.EF6.Migrations;

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
        public IDbSet<ModuleConnection> ModuleConnections { get; set; }

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// The modules store.
        /// </summary>
        public IDbSet<Module> Modules { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
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