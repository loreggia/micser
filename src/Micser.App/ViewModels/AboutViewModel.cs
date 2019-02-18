using Micser.App.Infrastructure;
using Micser.App.Properties;
using System.Reflection;

namespace Micser.App.ViewModels
{
    public class AboutViewModel : ViewModel
    {
        private string _aboutText;
        private string _versionText;

        public AboutViewModel()
        {
            AboutText = Resources.AboutText;
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            VersionText = $"{Resources.AboutVersionText} {version}";
        }

        public string AboutText
        {
            get => _aboutText;
            set => SetProperty(ref _aboutText, value);
        }

        public string VersionText
        {
            get => _versionText;
            set => SetProperty(ref _versionText, value);
        }
    }
}