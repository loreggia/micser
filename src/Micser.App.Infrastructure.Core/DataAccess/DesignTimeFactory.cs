using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Micser.App.Infrastructure.DataAccess
{
    public class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"ConnectionStrings:DefaultConnection", "DesignTimeAppDb.db"}
            });
            return new AppDbContext(configurationBuilder.Build());
        }
    }
}