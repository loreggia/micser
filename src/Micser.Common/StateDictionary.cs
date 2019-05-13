using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Micser.Common
{
    /// <summary>
    /// A string/object dictionary that provides typed access to the values.
    /// Used for generic state serialization.
    /// </summary>
    [Serializable]
    public class StateDictionary : Dictionary<string, object>
    {
        /// <inheritdoc />
        public StateDictionary()
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="StateDictionary"/> class and shallow copies the <paramref name="other"/> dictionary's values.
        /// </summary>
        public StateDictionary(StateDictionary other)
        {
            foreach (var key in other.Keys)
            {
                this[key] = other[key];
            }
        }

        /// <inheritdoc />
        protected StateDictionary(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets an object from the dictionary.
        /// If key is not present or the type is not convertible to the type <typeparamref name="T"/>,
        /// the value passed in <paramref name="defaultValue"/> is returned.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="defaultValue">The default value that is returned if the key is not found
        /// or the stored type is not convertible to the return type <typeparamref name="T"/>.</param>
        /// <typeparam name="T">The type of the value to get.</typeparam>
        public T GetObject<T>(string key, T defaultValue = default(T))
        {
            if (ContainsKey(key))
            {
                var obj = this[key];

                if (obj is T result)
                {
                    return result;
                }

                if (obj != null)
                {
                    var tType = typeof(T);
                    var objType = obj.GetType();

                    var objConverter = TypeDescriptor.GetConverter(objType);

                    if (objConverter.CanConvertTo(tType))
                    {
                        return (T)objConverter.ConvertTo(obj, tType);
                    }

                    var tConverter = TypeDescriptor.GetConverter(tType);

                    if (tConverter.CanConvertFrom(objType))
                    {
                        return (T)tConverter.ConvertFrom(objType);
                    }
                }
            }

            return defaultValue;
        }
    }
}