using System;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// Declares that the state of the widget is not automatically saved when this property is changed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class UnsavedAttribute : Attribute
    {
    }
}