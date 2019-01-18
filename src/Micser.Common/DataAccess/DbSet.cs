using System;
using System.Collections;
using System.Collections.Generic;

namespace Micser.Common.DataAccess
{
    public class DbSet<T> : IDbSet<T>
    {
        private readonly DataContext _context;
        private readonly List<T> _entities;

        public DbSet(DataContext context)
        {
            _context = context;
            _entities = new List<T>();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _entities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Insert(T entity)
        {
            _entities.Add(entity);
        }

        public void Update(T entity)
        {
            var index = _entities.IndexOf(entity);
            if (index < 0)
            {
                throw new InvalidOperationException("Entity has not been added.");
            }

            _entities[index] = entity;
        }
    }
}