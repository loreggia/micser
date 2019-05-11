using System;

namespace Micser.Common.DataAccess
{
    /// <inheritdoc cref="IUnitOfWorkFactory"/>
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly Func<IUnitOfWork> _factory;

        /// <summary>
        /// Creates an instance of the <see cref="UnitOfWorkFactory"/> class.
        /// </summary>
        /// <param name="factory">A factory function that creates the unit of work.</param>
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