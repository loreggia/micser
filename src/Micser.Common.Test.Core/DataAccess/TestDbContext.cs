using Microsoft.EntityFrameworkCore;
using Micser.Common.DataAccess;
using System.ComponentModel.DataAnnotations;

namespace Micser.Common.Test.DataAccess
{
    public interface ITestModelRepository : IRepository<TestModel>
    {
    }

    public class TestDbContext : DbContext
    {
        public DbSet<TestModel> TestModels { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data source=test.db");

            base.OnConfiguring(optionsBuilder);
        }
    }

    public class TestModel
    {
        public string Content { get; set; }

        [Key]
        public long Id { get; set; }
    }

    public class TestModelRepository : Repository<TestModel>, ITestModelRepository
    {
        public TestModelRepository(DbContext context)
            : base(context)
        {
        }

        public DbContext DbContext => Context;
    }

    public class TestUnitOfWork : UnitOfWork
    {
        public TestUnitOfWork(DbContext context, IRepositoryFactory repositoryFactory)
            : base(context, repositoryFactory)
        {
        }

        public DbContext DbContext => Context;
    }
}