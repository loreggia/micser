using Microsoft.EntityFrameworkCore;
using Micser.Common.DataAccess;
using Micser.Common.DataAccess.Repositories;
using System.ComponentModel.DataAnnotations;
using Unity;
using Xunit;

namespace Micser.Common.Test.DataAccess
{
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

    public class UnitOfWorkTest
    {
        [Fact]
        public void IsCreatingNewUoW()
        {
            var container = new UnityContainer();
            container.RegisterType<DbContext, TestDbContext>();
            container.RegisterInstance<IRepositoryFactory>(new RepositoryFactory(t => (IRepository)container.Resolve(t)));
            container.RegisterInstance<IUnitOfWorkFactory>(new UnitOfWorkFactory(() => container.Resolve<IUnitOfWork>()));
            container.RegisterType<IUnitOfWork, UnitOfWork>();

            container.RegisterType<ISettingValueRepository, SettingValueRepository>();

            var factory = container.Resolve<IUnitOfWorkFactory>();
            var uow1 = factory.Create();
            var repo1 = uow1.GetRepository<ISettingValueRepository>();
            uow1.Dispose();

            var uow2 = factory.Create();
            var repo2 = uow2.GetRepository<ISettingValueRepository>();
            uow2.Dispose();

            Assert.NotSame(uow1, uow2);
            Assert.NotSame(repo1, repo2);
        }

        [Fact]
        public void SaveData()
        {
            var testModel = new TestModel { Content = "Test1" };

            using (var db = new TestDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }

            using (var db = new TestDbContext())
            {
                db.TestModels.Add(testModel);
                db.SaveChanges();
            }

            Assert.NotEqual(0, testModel.Id);

            using (var db = new TestDbContext())
            {
                var result = db.TestModels.Find(testModel.Id);

                Assert.NotNull(result);
                Assert.Equal(testModel.Content, result.Content);
            }
        }
    }
}