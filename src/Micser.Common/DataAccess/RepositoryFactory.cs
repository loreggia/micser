using System;
using System.Data.Entity;

namespace Micser.Common.DataAccess
{
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly Func<Type, DbContext, object> _factory;

        public RepositoryFactory(Func<Type, DbContext, object> factory)
        {
            _factory = factory;
        }

        public T Create<T>(DbContext context)
            where T : class
        {
            return _factory(typeof(T), context) as T;
        }
    }
}