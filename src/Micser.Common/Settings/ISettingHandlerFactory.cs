using System;

namespace Micser.Common.Settings
{
    /// <summary>
    /// A factory for creating <see cref="ISettingHandler"/> instances.
    /// </summary>
    public interface ISettingHandlerFactory
    {
        /// <summary>
        /// Creates a setting handler of the specified type.
        /// </summary>
        /// <param name="type">The type of the setting handler. Has to implement <see cref="ISettingHandler"/>.</param>
        ISettingHandler Create(Type type);
    }
}