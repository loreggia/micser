using Microsoft.EntityFrameworkCore;
using System;

namespace Micser.Common.DataAccess
{
    public class DbContextFactory : IDbContextFactory
    {
        private readonly Func<DbContext> _factoryFunc;

        public DbContextFactory(Func<DbContext> factoryFunc)
        {
            _factoryFunc = factoryFunc;
        }

        public DbContext Create()
        {
            return _factoryFunc();
        }
    }
}