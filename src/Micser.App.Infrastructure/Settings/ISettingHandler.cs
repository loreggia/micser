using System.Collections.Generic;

namespace Micser.App.Infrastructure.Settings
{
    public interface IListSettingHandler : ISettingHandler
    {
        IDictionary<object, string> CreateList();
    }

    public interface ISettingHandler
    {
        object OnLoadSetting(object value);

        object OnSaveSetting(object value);
    }
}