using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common;

namespace Micser.Extensions
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, string connectionString)
            where TContext : DbContext
        {
            var directory = Globals.AppDataFolder;

#if DEBUG
            directory = Path.Combine(directory, "Debug");
#endif

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            connectionString = connectionString.Replace(Globals.ConnectionStringFolder, directory);

            void ConfigureDbContextOptions(DbContextOptionsBuilder options)
            {
                options.UseSqlite(connectionString);
            }

            //services.AddDbContext<TContext>(ConfigureDbContextOptions, ServiceLifetime.Transient);
            services.AddDbContextFactory<TContext>(ConfigureDbContextOptions);
            return services;
        }
    }
}