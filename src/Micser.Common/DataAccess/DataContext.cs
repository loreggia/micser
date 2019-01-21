using System;
using System.Collections.Generic;

namespace Micser.Common.DataAccess
{
    public class DataContext : IDisposable
    {
        private readonly Database _database;
        private readonly Dictionary<string, IDbSet> _tables;

        public DataContext(Database database)
        {
            _database = database;
            _tables = new Dictionary<string, IDbSet>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DataContext()
        {
            Dispose(false);
        }

        public IDbSet<T> GetCollection<T>(string table = null)
        {
            table = table ?? GetTableName<T>();
            if (_tables.ContainsKey(table) && _tables[table] is IDbSet<T> dbSet)
            {
                return dbSet;
            }

            dbSet = _database.GetDbSet<T>(table);
            _tables.Add(table, dbSet);
            return dbSet;
        }

        public virtual void Save()
        {
            _database.Save(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tables.Clear();
            }
        }

        protected virtual string GetTableName<T>()
        {
            return typeof(T).FullName;
        }

        public IEnumerable<IDbSet> GetChangedSets()
        {
            return _tables.Values;
        }
    }
}