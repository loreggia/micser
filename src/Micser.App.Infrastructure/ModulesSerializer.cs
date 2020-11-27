using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Micser.Common.Modules;
using Newtonsoft.Json;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Exports/imports (audio) module data to and from JSON files.
    /// </summary>
    public class ModulesSerializer
    {
        private readonly ILogger<ModulesSerializer> _logger;

        /// <inheritdoc />
        public ModulesSerializer(ILogger<ModulesSerializer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Serializes the <paramref name="data"/> to a file.
        /// </summary>
        /// <param name="fileName">The full file name to serialize the data to.</param>
        /// <param name="data">The data to serialize.</param>
        public bool Export(string fileName, ModulesExportDto data)
        {
            try
            {
                using (var fs = new StreamWriter(fileName))
                using (var jsonWriter = new JsonTextWriter(fs))
                {
                    var serializer = JsonSerializer.CreateDefault();
                    serializer.Serialize(jsonWriter, data);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Export to '{fileName}' failed.");
                return false;
            }
        }

        /// <summary>
        /// Deserializes module data from a file.
        /// </summary>
        /// <param name="fileName">The file name of the file containing the serialized module data.</param>
        public ModulesExportDto Import(string fileName)
        {
            try
            {
                using (var fs = new StreamReader(fileName))
                using (var jsonReader = new JsonTextReader(fs))
                {
                    var serializer = JsonSerializer.CreateDefault();
                    return serializer.Deserialize<ModulesExportDto>(jsonReader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Import from '{fileName}' failed.");
                return null;
            }
        }
    }
}