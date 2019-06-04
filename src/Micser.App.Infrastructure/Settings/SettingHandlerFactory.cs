using System;

namespace Micser.App.Infrastructure.Settings
{
    public class SettingHandlerFactory : ISettingHandlerFactory
    {
        private readonly Func<Type, ISettingHandler> _factoryMethod;

        public SettingHandlerFactory(Func<Type, ISettingHandler> factoryMethod)
        {
            _factoryMethod = factoryMethod;
        }

        public ISettingHandler Create(Type type)
        {
            return _factoryMethod(type);
        }
    }
}