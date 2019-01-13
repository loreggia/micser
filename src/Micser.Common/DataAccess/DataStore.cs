using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Micser.Common.DataAccess
{
    public class DataStore
    {
        private readonly Database _database;

        public DataStore(Database database)
        {
            Tables = new ConcurrentDictionary<string, object>();
            _database = database;
        }

        public IDictionary<string, object> Tables { get; }

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

        public void SetObject<T>(T value, string table = null)
        {
            table = table ?? GetTableName<T>();
            if (Tables.ContainsKey(table))
            {
                Tables[table] = value;
            }
            else
            {
                Tables.Add(table, value);
            }
        }

        private static string GetTableName<T>()
        {
            return typeof(T).FullName;
        }
    }
}