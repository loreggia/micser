using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Common.DataAccess
{
    public class DataContext : IDisposable
    {
        private readonly Database _database;
        private readonly Dictionary<string, IEnumerable> _tables;

        public DataContext(Database database)
        {
            _database = database;
            _tables = new Dictionary<string, IEnumerable>();
        }

        ~DataContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IEnumerable<T> GetCollection<T>(string table = null)
        {
            table = table ?? GetTableName<T>();
            if (_tables.ContainsKey(table))
            {
                return _tables[table].Cast<T>();
            }

            var dbSet = _database.GetDbSet<T>(table);
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
            }
        }

        protected virtual string GetTableName<T>()
        {
            return typeof(T).FullName;
        }
    }
}