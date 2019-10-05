using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Micser.Common;
using Micser.Common.DataAccess.Models;
using System.IO;

namespace Micser.App.Infrastructure.DataAccess
{
    /// <summary>
    /// The entity framework database context for the app.
    /// </summary>
    public class AppDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        /// <inheritdoc />
        public AppDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// The application setting store.
        /// </summary>
        public DbSet<SettingValue> Settings { get; set; }

        /// <inheritdoc />
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var cs = _configuration.GetConnectionString("DefaultConnection");
            var directory = Globals.AppDataFolder;

#if DEBUG
            directory = Path.Combine(directory, "Debug");
#endif

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            cs = cs.Replace(Globals.ConnectionStringFolder, directory);
            optionsBuilder.UseSqlite(cs);

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SettingValue>()
                .HasIndex(x => x.Key)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}