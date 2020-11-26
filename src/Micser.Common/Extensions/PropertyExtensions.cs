using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Micser.Common.Modules;

namespace Micser.Common.Extensions
{
    /// <summary>
    /// Contains helper extension methods for handling properties and property expressions.
    /// </summary>
    public static class PropertyExtensions
    {
        /// <summary>
        /// Gets the name of the property described by the specified expression.
        /// </summary>
        /// <param name="property">A property expression.</param>
        /// <typeparam name="T">The property type.</typeparam>
        /// <exception cref="ArgumentException"></exception>
        public static string GetName<T>(Expression<Func<T>> property)
        {
            if (!(property.Body is MemberExpression me))
            {
                throw new ArgumentException(nameof(property));
            }
            return me.Member.Name;
        }

        /// <summary>
        /// Gets all properties decorated with the <see cref="SaveStateAttribute"/> and saves their values in the <see cref="ModuleState.Data"/> property of the specified <paramref name="state"/>.
        /// </summary>
        /// <param name="obj">The object to get the state properties from.</param>
        /// <param name="state">The state to save the values to.</param>
        public static void GetStateProperties(this object obj, ModuleState state)
        {
            var properties = obj.GetStateProperties();
            foreach (var property in properties)
            {
                var value = property.Key.GetValue(obj) ?? property.Value.DefaultValue;

                state[property.Key.Name] = value.ToType<string>();
            }
        }

        /// <summary>
        /// Gets all properties decorated with the <see cref="SaveStateAttribute"/> on the specified object.
        /// </summary>
        /// <param name="obj">The object to get the state properties from.</param>
        /// <returns>A dictionary containing the <see cref="PropertyInfo"/> describing the property and a <see cref="SaveStateAttribute"/>.</returns>
        public static IDictionary<PropertyInfo, SaveStateAttribute> GetStateProperties(this object obj)
        {
            var result = from p in obj.GetType().GetProperties()
                         let a = p.GetCustomAttribute<SaveStateAttribute>()
                         where a != null
                         select new { p, a };
            return result.ToDictionary(x => x.p, x => x.a);
        }

        /// <summary>
        /// Loads/Sets the values saved in the <paramref name="state"/>'s <see cref="ModuleState.Data"/> to the corresponding properties on the object <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The object to set the state to.</param>
        /// <param name="state">The state containing the values to set to the object properties.</param>
        public static void SetStateProperties(this object obj, ModuleState state)
        {
            var properties = obj.GetStateProperties();
            foreach (var property in properties)
            {
                object value;
                var propertyType = property.Key.PropertyType;
                if (state.ContainsKey(property.Key.Name))
                {
                    value = state[property.Key.Name];
                }
                else
                {
                    value = property.Value.DefaultValue;
                }

                if (value != null && value.GetType() != propertyType)
                {
                    var valueType = value.GetType();

                    if (propertyType.IsEnum)
                    {
                        value = Enum.ToObject(propertyType, value);
                    }
                    else
                    {
                        var toConverter = TypeDescriptor.GetConverter(propertyType);
                        if (toConverter.CanConvertFrom(valueType))
                        {
                            value = toConverter.ConvertFrom(value);
                        }
                        else
                        {
                            var fromConverter = TypeDescriptor.GetConverter(valueType);
                            if (fromConverter.CanConvertTo(propertyType))
                            {
                                value = fromConverter.ConvertTo(value, propertyType);
                            }
                            else
                            {
                                value = property.Value.DefaultValue;
                            }
                        }
                    }
                }

                property.Key.SetValue(obj, value);
            }
        }
    }
}