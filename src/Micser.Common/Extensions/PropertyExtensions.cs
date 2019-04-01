using Micser.Common.Modules;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Micser.Common.Extensions
{
    public static class PropertyExtensions
    {
        public static string GetName<T>(Expression<Func<T>> property)
        {
            if (!(property.Body is MemberExpression me))
            {
                throw new ArgumentException(nameof(property));
            }
            return me.Member.Name;
        }

        public static void GetStateProperties(this object obj, ModuleState state)
        {
            var properties = obj.GetStateProperties();
            foreach (var property in properties)
            {
                var value = property.Key.GetValue(obj) ?? property.Value.DefaultValue;

                state.Data[property.Key.Name] = value;
            }
        }

        public static IDictionary<PropertyInfo, SaveStateAttribute> GetStateProperties(this object obj)
        {
            var result = from p in obj.GetType().GetProperties()
                         let a = p.GetCustomAttribute<SaveStateAttribute>()
                         where a != null
                         select new { p, a };
            return result.ToDictionary(x => x.p, x => x.a);
        }

        public static void SetStateProperties(this object obj, ModuleState state)
        {
            var properties = obj.GetStateProperties();
            foreach (var property in properties)
            {
                object value;
                var propertyType = property.Key.PropertyType;
                if (state.Data.ContainsKey(property.Key.Name))
                {
                    value = state.Data[property.Key.Name];
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