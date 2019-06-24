using Micser.App.Infrastructure;

namespace Micser.App.ViewModels
{
    public class LibraryInfo : Bindable
    {
        private bool _isLicenseOpen;
        private string _licenseText;
        private string _licenseUrl;
        private string _name;
        private string _url;

        public bool IsLicenseOpen
        {
            get => _isLicenseOpen;
            set => SetProperty(ref _isLicenseOpen, value);
        }

        public string LicenseText
        {
            get => _licenseText;
            set => SetProperty(ref _licenseText, value);
        }

        public string LicenseUrl
        {
            get => _licenseUrl;
            set => SetProperty(ref _licenseUrl, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }
    }
}