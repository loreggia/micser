using Microsoft.EntityFrameworkCore;
using Micser.Common.DataAccess.Entities;
using Micser.Engine.Infrastructure.DataAccess.Models;

namespace Micser.Engine.Infrastructure.DataAccess
{
    /// <summary>
    /// The EF database context for engine storage.
    /// </summary>
    public class EngineDbContext : DbContext
    {
        /// <inheritdoc />
        public EngineDbContext(DbContextOptions options)
            : base(options)
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
            modelBuilder.Entity<SettingValue>()
                .HasIndex(x => x.Key)
                .IsUnique();

            modelBuilder.Entity<Module>()
                .HasMany(m => m.SourceModuleConnections)
                .WithOne(c => c.SourceModule)
                .HasForeignKey(c => c.SourceModuleId);
            modelBuilder.Entity<Module>()
                .HasMany(m => m.TargetModuleConnections)
                .WithOne(c => c.TargetModule)
                .HasForeignKey(c => c.TargetModuleId);
        }
    }
}