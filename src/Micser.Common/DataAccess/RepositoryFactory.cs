using System;
using System.Data.Entity;

namespace Micser.Common.DataAccess
{
    /// <inheritdoc cref="IRepositoryFactory"/>
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly Func<Type, DbContext, IRepository> _factory;

        /// <summary>
        /// Creates an instance of the <see cref="RepositoryFactory"/> class.
        /// </summary>
        /// <param name="factory"></param>
        public RepositoryFactory(Func<Type, DbContext, IRepository> factory)
        {
            _factory = factory;
        }

        /// <inheritdoc />
        public T Create<T>(DbContext context)
            where T : class, IRepository
        {
            return _factory(typeof(T), context) as T;
        }
    }
}