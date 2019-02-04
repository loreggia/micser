using Micser.Engine.Infrastructure.DataAccess.Models;
using SQLite.CodeFirst;
using System.Data.Entity;

namespace Micser.Engine.Infrastructure.DataAccess
{
    public class EngineDbContext : DbContext
    {
        public EngineDbContext()
            : base("DefaultConnection")
        {
        }

        public IDbSet<ModuleConnection> ModuleConnections { get; set; }
        public IDbSet<Module> Modules { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new SqliteCreateDatabaseIfNotExists<EngineDbContext>(modelBuilder));

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