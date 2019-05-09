using System;
using WixSharp;
using WixSharp.Forms;
using Action = WixSharp.Action;

namespace Micser.Setup
{
    internal class Program
    {
        private static void Main()
        {
            bool FileFilter(string file)
            {
                return !file.EndsWith(".test.dll", StringComparison.InvariantCultureIgnoreCase) &&
                       !file.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase) &&
                       !file.EndsWith(".pdb", StringComparison.InvariantCultureIgnoreCase) &&
                       !file.EndsWith("Micser.DriverUtility.exe", StringComparison.InvariantCultureIgnoreCase);
            }

            var project = new ManagedProject("Micser")
            {
                GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b"),
                ManagedUI = new ManagedUI()
            };

#if DEBUG
            project.SourceBaseDir = @"..\..\bin\Debug\";
#else
            project.SourceBaseDir = @"..\..\bin\Release\";
#endif

            project.DigitalSignature = new DigitalSignature
            {
                PfxFilePath = @"..\..\crt\Certificate.pfx",
                Password = "testing",
                Description = "Micser",
                TimeUrl = new Uri("http://timestamp.verisign.com/scripts/timstamp.dll")
            };

            project.Dirs = new[]
            {
                new Dir(@"%ProgramFiles%\Micser",
                    new Files(@"App\*.*", FileFilter),
                    new File(new Id("DriverUtilityExe"), @"App\Micser.DriverUtility.exe"),
                    new Files(@"Driver\Micser.Vac.Package\*.*")
                ),
                new Dir(@"%ProgramMenu%\Micser",
                    new ExeFileShortcut("Micser", @"[INSTALLDIR]\Micser.App.exe", ""),
                    new ExeFileShortcut("Uninstall Micser", "[SystemFolder]msiexec.exe", "/x [ProductCode]")
                )
            };

            project.Binaries = new[]
            {
                new Binary(new Id("DevconExe"), @"Driver\devcon.exe")
            };

            project.Actions = new Action[]
            {
                new BinaryFileAction("DevconExe", @"install ""[INSTALLDIR]Micser.Vac.Driver.inf"" Root\Micser.Vac.Driver", Return.check, When.After, Step.InstallFiles, Condition.NOT_Installed)
                {
                    Execute = Execute.deferred,
                    Impersonate = false
                },
                new BinaryFileAction("DevconExe", @"remove Root\Micser.Vac.Driver", Return.check, When.Before, Step.RemoveFiles, Condition.BeingUninstalled)
                {
                    Execute = Execute.deferred,
                    Impersonate = false
                },
                new InstalledFileAction("DriverUtilityExe", @"/c 1 /s", Return.check, When.After, Step.InstallFiles, Condition.NOT_Installed)
                {
                    Execute = Execute.deferred,
                    Impersonate = false
                },
            };

            project.ManagedUI.InstallDialogs.Add(Dialogs.Welcome)
                   .Add(Dialogs.Licence)
                   .Add(Dialogs.SetupType)
                   .Add(Dialogs.Features)
                   .Add(Dialogs.InstallDir)
                   .Add(Dialogs.Progress)
                   .Add(Dialogs.Exit);

            project.ManagedUI.ModifyDialogs.Add(Dialogs.MaintenanceType)
                   .Add(Dialogs.Features)
                   .Add(Dialogs.Progress)
                   .Add(Dialogs.Exit);

            project.BuildMsi();
        }
    }
}