using System;

namespace Micser.Common.DataAccess
{
    /// <inheritdoc cref="IUnitOfWorkFactory"/>
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly Func<IUnitOfWork> _factory;

        /// <inheritdoc />
        public UnitOfWorkFactory(Func<IUnitOfWork> factory)
        {
            _factory = factory;
        }

        /// <inheritdoc />
        public IUnitOfWork Create()
        {
            return _factory();
        }
    }
}