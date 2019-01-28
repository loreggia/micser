using System.Collections.Generic;

namespace Micser.Common
{
    public class StateDictionary : Dictionary<string, object>
    {
        public T GetObject<T>(string key, T defaultValue = default(T))
        {
            if (ContainsKey(key))
            {
                var obj = this[key];
                return obj is T result ? result : defaultValue;
            }

            return defaultValue;
        }
    }
}