using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Micser.Engine.Infrastructure.DataAccess
{
    // ReSharper disable once UnusedMember.Global
    public class DesignTimeFactory : IDesignTimeDbContextFactory<EngineDbContext>
    {
        public EngineDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder();
            builder.UseSqlite("DesignTimeEngineDb.db");
            return new EngineDbContext(builder.Options);
        }
    }
}