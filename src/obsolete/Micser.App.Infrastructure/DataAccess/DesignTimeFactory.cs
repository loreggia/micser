using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Micser.App.Infrastructure.DataAccess
{
    // ReSharper disable once UnusedMember.Global
    public class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder();
            builder.UseSqlite("DesignTimeAppDb.db");
            return new AppDbContext(builder.Options);
        }
    }
}