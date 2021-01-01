using Microsoft.EntityFrameworkCore;
using Micser.DataAccess.Entities;

namespace Micser.DataAccess
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
        public DbSet<ModuleConnectionEntity> ModuleConnections { get; set; }

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// The modules store.
        /// </summary>
        public DbSet<ModuleEntity> Modules { get; set; }

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

            var module = modelBuilder.Entity<ModuleEntity>();
            module.HasMany(m => m.SourceModuleConnections)
                .WithOne(c => c.SourceModule)
                .HasForeignKey(c => c.SourceModuleId);
            module.HasMany(m => m.TargetModuleConnections)
                .WithOne(c => c.TargetModule)
                .HasForeignKey(c => c.TargetModuleId);
        }
    }
}