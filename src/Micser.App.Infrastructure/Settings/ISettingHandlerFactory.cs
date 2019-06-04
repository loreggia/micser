using System;

namespace Micser.App.Infrastructure.Settings
{
    public interface ISettingHandlerFactory
    {
        ISettingHandler Create(Type type);
    }
}