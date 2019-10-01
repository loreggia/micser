using Microsoft.EntityFrameworkCore;
using Micser.Common.DataAccess;
using Micser.Common.DataAccess.Repositories;
using Unity;
using Xunit;

namespace Micser.Common.Test.DataAccess
{
    public class TestDbContext : DbContext
    {
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
    }
}