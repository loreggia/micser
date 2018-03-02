using System;
using Newtonsoft.Json.Linq;

namespace Micser.Infrastructure
{
    public interface IConfigurationService
    {
        T GetSetting<T>(string key, T defaultValue = default(T), Func<JObject, T> deserializer = null);

        T GetSetting<T>(string key, T defaultValue = default(T), Func<JArray, T> deserializer = null);

        bool Load(string fileName = null);

        bool Save(string fileName = null);

        void SetSetting<T>(string key, T value);
    }
}