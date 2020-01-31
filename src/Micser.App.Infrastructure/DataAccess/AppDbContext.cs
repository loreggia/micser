using Microsoft.EntityFrameworkCore;
using Micser.Common.DataAccess.Entities;

namespace Micser.App.Infrastructure.DataAccess
{
    /// <summary>
    /// The entity framework database context for the app.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <inheritdoc />
        public AppDbContext(DbContextOptions options)
            : base(options)
        {
        }

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// The application setting store.
        /// </summary>
        public DbSet<SettingValue> Settings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SettingValue>()
                .HasIndex(x => x.Key)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}