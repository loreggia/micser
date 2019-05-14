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
            _aboutText = Resources.AboutText;
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            _versionText = $"{Resources.AboutVersionText} {version}";
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