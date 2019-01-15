using Micser.Common.Extensions;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;

namespace Micser.Common.DataAccess
{
    public class Database : IDatabase
    {
        private readonly string _fileName;
        private readonly object _lock = new object();
        private readonly ILogger _logger;

        public Database(string fileName, ILogger logger)
        {
            _fileName = fileName;
            _logger = logger;
        }

        public DataContext GetContext()
        {
            if (!File.Exists(_fileName))
            {
                return new DataContext(this);
            }

            try
            {
                using (var reader = new StreamReader(_fileName))
                {
                    var tables = JsonConvert.DeserializeObject<Dictionary<string, object>>(reader.ReadToEnd());
                    var dataStore = new DataContext(this);
                    tables.ForEach(p => dataStore.Tables.Add(p.Key, p.Value));
                    return dataStore;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex);
            }

            return new DataContext(this);
        }

        internal void Save(DataContext dataContext)
        {
            try
            {
                using (var writer = new StreamWriter(_fileName))
                {
                    var json = JsonConvert.SerializeObject(dataContext.Tables);
                    writer.Write(json);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex);
            }
        }
    }
}