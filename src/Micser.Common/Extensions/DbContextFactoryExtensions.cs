using Microsoft.EntityFrameworkCore;
using Micser.Common.DataAccess;

namespace Micser.Common.Extensions
{
    public static class DbContextFactoryExtensions
    {
        public static TDbContext Create<TDbContext>(this IDbContextFactory factory)
            where TDbContext : DbContext
        {
            return (TDbContext)factory.Create();
        }
    }
}