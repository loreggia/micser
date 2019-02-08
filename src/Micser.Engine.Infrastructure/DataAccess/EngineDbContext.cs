using Micser.Engine.Infrastructure.DataAccess.Models;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.SQLite.EF6.Migrations;

namespace Micser.Engine.Infrastructure.DataAccess
{
    public class ContextMigrationConfiguration : DbMigrationsConfiguration<EngineDbContext>
    {
        public ContextMigrationConfiguration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            SetSqlGenerator("System.Data.SQLite", new SQLiteMigrationSqlGenerator());
        }
    }

    public class EngineDbContext : DbContext
    {
        static EngineDbContext()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<EngineDbContext, ContextMigrationConfiguration>(true));
        }

        public EngineDbContext()
            : base("DefaultConnection")
        {
        }

        public IDbSet<ModuleConnection> ModuleConnections { get; set; }
        public IDbSet<Module> Modules { get; set; }

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