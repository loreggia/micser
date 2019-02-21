using Micser.App.Infrastructure.DataAccess;
using Micser.App.Infrastructure.DataAccess.Repositories;
using Micser.Common.DataAccess;
using System.Data.Entity;
using Unity;
using Unity.Resolution;
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
            container.RegisterType<DbContext, AppDbContext>();
            container.RegisterInstance<IRepositoryFactory>(new RepositoryFactory((t, c) => container.Resolve(t, new ParameterOverride("context", c))));
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