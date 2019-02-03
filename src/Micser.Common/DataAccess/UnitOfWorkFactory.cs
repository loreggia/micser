using System;

namespace Micser.Common.DataAccess
{
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly Func<IUnitOfWork> _factory;

        public UnitOfWorkFactory(Func<IUnitOfWork> factory)
        {
            _factory = factory;
        }

        public IUnitOfWork Create()
        {
            return _factory();
        }
    }
}