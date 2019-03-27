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
                var value = property.Key.GetValue(obj);
                if (value == null)
                {
                    value = property.Value.DefaultValue;
                }
                else
                {
                    var type = value.GetType();
                    var typeDefault = type.IsValueType ? Activator.CreateInstance(type) : null;

                    if (Equals(value, typeDefault))
                    {
                        value = property.Value.DefaultValue;
                    }
                }

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
                if (state.Data.ContainsKey(property.Key.Name))
                {
                    value = state.Data[property.Key.Name];
                }
                else
                {
                    value = property.Value.DefaultValue;
                }

                if (value != null && value.GetType() != property.Key.PropertyType)
                {
                    var toConverter = TypeDescriptor.GetConverter(property.Key.PropertyType);
                    if (toConverter.CanConvertFrom(value.GetType()))
                    {
                        value = toConverter.ConvertFrom(value);
                    }
                    else
                    {
                        var fromConverter = TypeDescriptor.GetConverter(value.GetType());
                        if (fromConverter.CanConvertTo(property.Key.PropertyType))
                        {
                            value = fromConverter.ConvertTo(value, property.Key.PropertyType);
                        }
                        else
                        {
                            value = property.Value.DefaultValue;
                        }
                    }
                }

                property.Key.SetValue(obj, value);
            }
        }
    }
}