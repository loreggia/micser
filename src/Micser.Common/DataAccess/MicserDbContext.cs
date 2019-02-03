using Micser.Common.DataAccess.Models;
using SQLite.CodeFirst;
using System.Data.Entity;

namespace Micser.Common.DataAccess
{
    public class MicserDbContext : DbContext
    {
        public MicserDbContext()
            : base("DefaultConnection")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new SqliteCreateDatabaseIfNotExists<MicserDbContext>(modelBuilder));

            modelBuilder.Entity<Module>()
                .HasMany(m => m.FromModuleConnections)
                .WithRequired(c => c.FromModule)
                .HasForeignKey(c => c.FromModuleId);
            modelBuilder.Entity<Module>()
                .HasMany(m => m.ToModuleConnections)
                .WithRequired(c => c.ToModule)
                .HasForeignKey(c => c.ToModuleId);
        }
    }
}