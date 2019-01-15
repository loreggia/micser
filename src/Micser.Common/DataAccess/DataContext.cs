using System;
using System.Collections.Generic;

namespace Micser.Common.DataAccess
{
    public abstract class DataContext : IDisposable
    {
        private readonly Database _database;

        protected DataContext(Database database)
        {
            _database = database;
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
            return GetObject<IEnumerable<T>>(table) ?? new T[0];
        }

        public T GetObject<T>(string tableName = null)
        {
            tableName = tableName ?? GetTableName<T>();

            return default(T);
        }

        public virtual void Save()
        {
            _database.Save(this);
        }

        public virtual void SetObject<T>(T value, string tableName = null)
        {
            tableName = tableName ?? GetTableName<T>();
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