using System;

namespace Micser.Common.Modules
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SaveStateAttribute : Attribute
    {
        public SaveStateAttribute(object defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public object DefaultValue { get; }
    }
}