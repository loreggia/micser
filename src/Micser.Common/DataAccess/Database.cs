﻿using Newtonsoft.Json;
using NLog;
using System;
using System.IO;

namespace Micser.Infrastructure.DataAccess
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

        public DataStore GetContext()
        {
            if (!File.Exists(_fileName))
            {
                return new DataStore(this);
            }

            try
            {
                using (var reader = new StreamReader(_fileName))
                {
                    var result = JsonConvert.DeserializeObject<DataStore>(reader.ReadToEnd());
                    result.Initialize(this);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex);
            }

            return new DataStore(this);
        }

        internal void Save(DataStore dataStore)
        {
            try
            {
                using (var writer = new StreamWriter(_fileName))
                {
                    var json = JsonConvert.SerializeObject(dataStore);
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