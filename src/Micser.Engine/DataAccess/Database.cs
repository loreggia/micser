using Unity;

namespace Micser.Engine.DataAccess
{
    internal class Database : IDatabase
    {
        private readonly IUnityContainer _container;

        public Database(IUnityContainer container)
        {
            _container = container;
        }

        public DbContext GetContext()
        {
            return _container.Resolve<DbContext>();
        }
    }
}