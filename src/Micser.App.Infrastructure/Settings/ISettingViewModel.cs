namespace Micser.App.Infrastructure.Settings
{
    public interface ISettingViewModel
    {
        SettingDefinition Definition { get; }
        bool IsEnabled { get; set; }
    }
}