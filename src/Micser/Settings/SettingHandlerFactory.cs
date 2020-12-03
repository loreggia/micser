using System;
using Micser.Common.Settings;

namespace Micser.Settings
{
    /// <inheritdoc cref="ISettingHandlerFactory" />
    public class SettingHandlerFactory : ISettingHandlerFactory
    {
        private readonly Func<Type, ISettingHandler> _factoryMethod;

        /// <inheritdoc />
        public SettingHandlerFactory(Func<Type, ISettingHandler> factoryMethod)
        {
            _factoryMethod = factoryMethod;
        }

        /// <inheritdoc />
        public ISettingHandler Create(Type type)
        {
            return _factoryMethod(type);
        }
    }
}