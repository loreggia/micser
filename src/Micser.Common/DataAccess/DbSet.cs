using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Common.DataAccess
{
    public class DbSet<T> : IDbSet<T>
    {
        private readonly Dictionary<object, DbEntry<T>> _entities;

        public DbSet(string name)
        {
            Name = name;
            _entities = new Dictionary<object, DbEntry<T>>();
        }

        public string Name { get; }

        public void Delete<TId>(TId id)
        {
            if (_entities.TryGetValue(id, out var dbEntry))
            {
                dbEntry.State = EntryState.Deleted;
            }
        }

        public T GetById<TId>(TId id)
        {
            if (_entities.TryGetValue(id, out var dbEntry))
            {
                return dbEntry.Entity;
            }

            return default(T);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _entities.Values.Select(e => e.Entity).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _entities.Values.GetEnumerator();
        }

        public void Initialize(IEnumerable<DbEntry<T>> dbEntries)
        {
            _entities.Clear();

            foreach (var dbEntry in dbEntries)
            {
                _entities.Add(dbEntry.Id, dbEntry);
            }
        }

        public void Insert(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var dbEntry = new DbEntry<T>(entity)
            {
                State = EntryState.Added
            };
            if (_entities.ContainsKey(dbEntry.Id))
            {
                throw new InvalidOperationException($"An entity with ID {dbEntry.Id} is already part of the DB set.");
            }

            _entities.Add(dbEntry.Id, dbEntry);
        }

        public void Update(T entity)
        {
            var dbEntry = new DbEntry<T>(entity)
            {
                State = EntryState.Changed
            };
            if (!_entities.ContainsKey(dbEntry.Id))
            {
                throw new InvalidOperationException("Entity has not been added.");
            }

            _entities[dbEntry.Id] = dbEntry;
        }
    }
}