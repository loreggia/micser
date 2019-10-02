using Micser.App.Infrastructure;
using Micser.App.Resources;
using Newtonsoft.Json;
using Prism.Commands;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Micser.App.ViewModels
{
    public class AboutViewModel : ViewModelNavigationAware
    {
        private string _aboutText;
        private IEnumerable<LibraryInfo> _libraries;
        private string _versionText;

        public AboutViewModel()
        {
            _aboutText = Strings.AboutText;
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            _versionText = $"{Strings.AboutVersionText} {version}";

            LoadLicenseCommand = new DelegateCommand<LibraryInfo>(LoadLicense);
            OpenLibraryUrlCommand = new DelegateCommand<LibraryInfo>(OpenLibraryUrl);
            ClosePopupCommand = new DelegateCommand<LibraryInfo>(ClosePopup);
        }

        public string AboutText
        {
            get => _aboutText;
            set => SetProperty(ref _aboutText, value);
        }

        public ICommand ClosePopupCommand { get; }

        public IEnumerable<LibraryInfo> Libraries
        {
            get => _libraries;
            set => SetProperty(ref _libraries, value);
        }

        public ICommand LoadLicenseCommand { get; }

        public ICommand OpenLibraryUrlCommand { get; }

        public string VersionText
        {
            get => _versionText;
            set => SetProperty(ref _versionText, value);
        }

        protected override async void OnNavigatedTo(object parameter)
        {
            base.OnNavigatedTo(parameter);

            await LoadLibrariesAsync();
        }

        private static void ClosePopup(LibraryInfo libraryInfo)
        {
            libraryInfo.IsLicenseOpen = false;
        }

        private static async void LoadLicense(LibraryInfo libraryInfo)
        {
            if (libraryInfo == null)
            {
                return;
            }

            libraryInfo.IsLicenseOpen = true;

            if (!string.IsNullOrEmpty(libraryInfo.LicenseUrl))
            {
                using (var client = new WebClient())
                {
                    var licenseText = await client.DownloadStringTaskAsync(libraryInfo.LicenseUrl);

                    // open HTML response in browser
                    if (licenseText != null && licenseText.StartsWith("<!"))
                    {
                        Process.Start(libraryInfo.LicenseUrl);
                        libraryInfo.IsLicenseOpen = false;
                        return;
                    }

                    libraryInfo.LicenseText = licenseText;
                }
            }
            else if (string.IsNullOrEmpty(libraryInfo.LicenseText))
            {
                libraryInfo.LicenseText = "No license available.";
            }
        }

        private static void OpenLibraryUrl(LibraryInfo libraryInfo)
        {
            Process.Start(libraryInfo.Url);
        }

        private Task LoadLibrariesAsync()
        {
            return Task.Run(() =>
            {
                using (var libJsonStream = new MemoryStream(Files.libraries))
                using (var streamReader = new StreamReader(libJsonStream))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    var serializer = JsonSerializer.CreateDefault();
                    Libraries = serializer.Deserialize<LibraryInfo[]>(jsonReader);
                }
            });
        }
    }
}