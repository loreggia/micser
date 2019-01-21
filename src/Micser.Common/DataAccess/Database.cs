using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Micser.Common.DataAccess
{
    public class Database : IDatabase
    {
        private readonly string _fileName;
        private readonly object _lock = new object();
        private readonly ILogger _logger;
        private JObject _dbInstance;

        public Database(string fileName, ILogger logger)
        {
            _fileName = fileName;
            _logger = logger;
        }

        public DataContext GetContext()
        {
            Load();

            return new DataContext(this);
        }

        internal DbSet<T> GetDbSet<T>(string table)
        {
            var dbSet = new DbSet<T>(table);

            if (_dbInstance.TryGetValue(table, StringComparison.InvariantCultureIgnoreCase, out var token))
            {
                var entities = token.ToObject<IEnumerable<T>>();
                var dbEntries = entities.Select(e => new DbEntry<T>(e));
                dbSet.Initialize(dbEntries);
            }

            return dbSet;
        }

        private void Load()
        {
            try
            {
                lock (_lock)
                {
                    if (!File.Exists(_fileName))
                    {
                        _dbInstance = new JObject();
                        return;
                    }

                    using (var reader = new StreamReader(_fileName))
                    using (var jsonReader = new JsonTextReader(reader))
                    {
                        _dbInstance = JObject.Load(jsonReader);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex);
            }
        }

        internal void Save(DataContext dataContext)
        {
            try
            {
                using (var writer = new StreamWriter(_fileName))
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    var dbSets = dataContext.GetChangedSets();
                    foreach (var dbSet in dbSets)
                    {
                        _dbInstance[dbSet.Name] = JToken.FromObject(dbSet.Cast<object>());
                    }
                    _dbInstance.WriteTo(jsonWriter);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex);
            }
        }
    }
}