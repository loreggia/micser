using System.ComponentModel;
using System.Configuration.Install;

namespace Micser.Engine
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}