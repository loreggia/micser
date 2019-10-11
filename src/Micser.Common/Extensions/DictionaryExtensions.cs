using System.Collections;

namespace Micser.Common.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetObject<TValue>(this IDictionary dictionary, string key, TValue defaultValue = default)
        {
            return dictionary.GetObject<string, TValue>(key, defaultValue);
        }

        /// <summary>
        /// Gets an object from the dictionary.
        /// If key is not present or the type is not convertible to the type <typeparamref name="TValue"/>,
        /// the value passed in <paramref name="defaultValue"/> is returned.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="defaultValue">The default value that is returned if the key is not found or the stored type is not convertible to the return type <typeparamref name="TValue"/>.</param>
        public static TValue GetObject<TKey, TValue>(this IDictionary dictionary, TKey key, TValue defaultValue = default)
        {
            if (dictionary.Contains(key))
            {
                var obj = dictionary[key];

                if (obj is TValue result)
                {
                    return result;
                }

                if (obj != null)
                {
                    return obj.ToType<TValue>();
                }
            }

            return defaultValue;
        }
    }
}