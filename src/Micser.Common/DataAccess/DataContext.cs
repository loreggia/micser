using System;
using System.Collections.Generic;
using System.Linq;

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

        ~DataContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IEnumerable<IDbSet> GetChangedSets()
        {
            var dbSets = _tables.Values;
            var result = from dbSet in dbSets
                         let dbEntries = dbSet.Cast<DbEntry>()
                         where dbEntries.Any(e => e.State != EntryState.Unchanged)
                         select dbSet;
            return result;
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
            foreach (var dbSet in _tables.Values)
            {
                var deleted = dbSet.Cast<DbEntry>().Where(e => e.State == EntryState.Deleted);
                //todo
            }
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
            return typeof(T).AssemblyQualifiedName;
        }
    }
}