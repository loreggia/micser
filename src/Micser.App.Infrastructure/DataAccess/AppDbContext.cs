using SQLite.CodeFirst;
using System.Data.Entity;

namespace Micser.App.Infrastructure.DataAccess
{
    public class AppDbContext : DbContext
    {
        public AppDbContext()
            : base("DefaultConnection")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new SqliteCreateDatabaseIfNotExists<AppDbContext>(modelBuilder));
        }
    }
}