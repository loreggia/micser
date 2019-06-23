using System;
using System.Linq;
using System.Reflection;
using System.Windows.Markup;

namespace Micser.App.Infrastructure.Extensions
{
    /// <summary>
    /// A XAML markup extension similar to the <c>nameof</c> operator.
    /// </summary>
    [ContentProperty(nameof(Member))]
    public class NameOfExtension : MarkupExtension
    {
        /// <summary>
        /// The member whose name is returned.
        /// </summary>
        public string Member { get; set; }

        /// <summary>
        /// The type containing the <see cref="Member"/>.
        /// </summary>
        public Type Type { get; set; }

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (Type == null || string.IsNullOrEmpty(Member) || Member.Contains("."))
            {
                throw new ArgumentException("Syntax for x:NameOf is Type={x:Type [className]} Member=[propertyName]");
            }

            var pinfo = Type.GetRuntimeProperties().FirstOrDefault(pi => pi.Name == Member);
            var finfo = Type.GetRuntimeFields().FirstOrDefault(fi => fi.Name == Member);

            if (pinfo == null && finfo == null)
            {
                throw new ArgumentException($"No property or field found for {Member} in {Type}");
            }

            return Member;
        }
    }
}