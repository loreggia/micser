using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Micser.Common.DataAccess
{
    public class DataStore
    {
        private Database _database;

        public DataStore(Database database)
        {
            Tables = new ConcurrentDictionary<string, object>();

            Initialize(database);
        }

        public IDictionary<string, object> Tables { get; set; }

        public IEnumerable<T> GetCollection<T>(string table = null)
        {
            table = table ?? GetTableName<T>();
            return GetObject<IEnumerable<T>>(table) ?? new T[0];
        }

        public T GetObject<T>(string table = null)
        {
            table = table ?? GetTableName<T>();
            if (Tables.ContainsKey(table) && Tables[table] is T)
            {
                return (T)Tables[table];
            }

            return default(T);
        }

        public void Save()
        {
            _database.Save(this);
        }

        internal void Initialize(Database database)
        {
            _database = database;
        }

        private static string GetTableName<T>()
        {
            return typeof(T).FullName;
        }
    }
}