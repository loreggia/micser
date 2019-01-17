using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.IO;

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
            return new DataContext(this);
        }

        internal DbSet<T> GetDbSet<T>(string table)
        {
            var dbSet = new DbSet<T>();
        }

        internal void Load()
        {
            try
            {
                using (var reader = new StreamReader(_fileName))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    _dbInstance = JObject.Load(jsonReader);
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
                dataContext.

                using (var writer = new StreamWriter(_fileName))
                using (var jsonWriter = new JsonTextWriter(writer))
                {
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