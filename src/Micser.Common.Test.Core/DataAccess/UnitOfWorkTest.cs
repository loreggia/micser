using Microsoft.EntityFrameworkCore;
using Micser.Common.DataAccess;
using Micser.Common.Extensions;
using Xunit;

namespace Micser.Common.Test.DataAccess
{
    public class UnitOfWorkTest
    {
        [Fact]
        public void IsCreatingNewUoW()
        {
            var container = new UnityContainerProvider();
            container.RegisterType<DbContext, TestDbContext>();
            container.RegisterInstance<IRepositoryFactory>(new RepositoryFactory((t, c) => (IRepository)container.Resolve(t, null, new DependencyOverride<DbContext>(c))));
            container.RegisterInstance<IUnitOfWorkFactory>(new UnitOfWorkFactory(() => container.Resolve<IUnitOfWork>()));
            container.RegisterType<IUnitOfWork, UnitOfWork>();

            container.RegisterType<ITestModelRepository, TestModelRepository>();

            var factory = container.Resolve<IUnitOfWorkFactory>();
            var uow1 = factory.Create();
            var repo1 = uow1.GetRepository<ITestModelRepository>();
            uow1.Dispose();

            var uow2 = factory.Create();
            var repo2 = uow2.GetRepository<ITestModelRepository>();
            uow2.Dispose();

            Assert.NotSame(uow1, uow2);
            Assert.NotSame(repo1, repo2);
        }

        [Fact]
        public void IsSameDbContext()
        {
            var container = new UnityContainerProvider();
            container.RegisterType<DbContext, TestDbContext>();
            container.RegisterInstance<IRepositoryFactory>(new RepositoryFactory((t, c) => (IRepository)container.Resolve(t, null, new DependencyOverride<DbContext>(c))));
            container.RegisterInstance<IUnitOfWorkFactory>(new UnitOfWorkFactory(() => container.Resolve<IUnitOfWork>()));
            container.RegisterType<IUnitOfWork, TestUnitOfWork>();

            container.RegisterType<ITestModelRepository, TestModelRepository>();

            var factory = container.Resolve<IUnitOfWorkFactory>();
            using (var uow = factory.Create())
            {
                var uowContext = ((TestUnitOfWork)uow).DbContext;

                var repository = uow.GetRepository<ITestModelRepository>();
                var repositoryContext = ((TestModelRepository)repository).DbContext;

                Assert.Same(uowContext, repositoryContext);
            }
        }

        [Fact]
        public void IsSavingData()
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