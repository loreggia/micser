using System.Collections.Generic;

namespace Micser.Common
{
    /// <summary>
    /// A string/object dictionary that provides typed access to the values.
    /// Used for generic state serialization.
    /// </summary>
    public class StateDictionary : Dictionary<string, object>
    {
        public StateDictionary()
        {
        }

        public StateDictionary(StateDictionary other)
        {
            foreach (var key in other.Keys)
            {
                this[key] = other[key];
            }
        }

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