using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Micser.Engine.Infrastructure.DataAccess
{
    // ReSharper disable once UnusedMember.Global
    public class DesignTimeFactory : IDesignTimeDbContextFactory<EngineDbContext>
    {
        public EngineDbContext CreateDbContext(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"ConnectionStrings:DefaultConnection", "DesignTimeEngineDb.db"}
            });
            return new EngineDbContext(configurationBuilder.Build());
        }
    }
}