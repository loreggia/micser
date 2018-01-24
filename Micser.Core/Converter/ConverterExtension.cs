using System;
using System.Windows.Markup;

namespace Micser.Core.Converter
{
    public class ConverterExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}