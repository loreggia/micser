using System;
using System.Linq;
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
                var excludedFiles = new[]
                {
                    ".test.dll",
                    ".pdb",
                    "Micser.App.exe",
                    "Micser.DriverUtility.exe",
                    "Micser.Engine.exe"
                };

                return !excludedFiles.Any(x => file.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));
            }

            var project = new ManagedProject("Micser")
            {
                GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b"),
                ManagedUI = new ManagedUI(),
                Version = new Version(1, 0, 0, 0),
                MajorUpgrade = MajorUpgrade.Default
            };

#if DEBUG
            project.SourceBaseDir = @"..\..\bin\Debug\";
#else
            project.SourceBaseDir = @"..\..\bin\Release\";

            project.DigitalSignature = new DigitalSignature
            {
                PfxFilePath = @"..\..\crt\Certificate.pfx",
                Password = "testing",
                Description = "Micser",
                TimeUrl = new Uri("http://timestamp.verisign.com/scripts/timstamp.dll")
            };
#endif

            project.Dirs = new[]
            {
                new Dir(@"%ProgramFiles%\Micser",
                    new Files(@"App\*.*", FileFilter),
                    new Files(@"Driver\Micser.Vac.Package\*.*"),
                    new File(new Id("MicserAppExe"), @"App\Micser.App.exe"),
                    new File(new Id("MicserDriverUtilityExe"), @"App\Micser.DriverUtility.exe"),
                    new File(new Id("MicserEngineExe"), @"App\Micser.Engine.exe")
                    {
                        ServiceInstaller = new ServiceInstaller("Micser.Engine")
                        {
                            StartOn = SvcEvent.Install,
                            StopOn = SvcEvent.InstallUninstall_Wait,
                            RemoveOn = SvcEvent.Uninstall_Wait,
                            Type = SvcType.ownProcess,
                            Account = "LocalSystem",
                            Description = "Micser Audio Engine Service",
                            DisplayName = "Micser Engine",
                            Start = SvcStartType.auto,
                            DelayedAutoStart = false,
                            Vital = true,
                            Interactive = false
                        }
                    }
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
                new ManagedAction(DriverActions.Install, typeof(DriverActions).Assembly.Location, Return.check, When.After, Step.InstallServices, Condition.NOT_Installed) { Rollback = nameof(DriverActions.Uninstall) },
                new ManagedAction(DriverActions.Uninstall, typeof(DriverActions).Assembly.Location, Return.check, When.Before, Step.RemoveFiles, Condition.BeingUninstalled) { Rollback = nameof(DriverActions.Install) },
                new ManagedAction(DriverActions.Configure, typeof(DriverActions).Assembly.Location, Return.check, When.After, Step.InstallServices, Condition.NOT_Installed),
                //new BinaryFileAction("DevconExe", @"install ""[INSTALLDIR]Micser.Vac.Driver.inf"" Root\Micser.Vac.Driver", Return.check, When.After, Step.InstallServices, Condition.NOT_Installed)
                //{
                //    Execute = Execute.deferred,
                //    Impersonate = false
                //},
                //new BinaryFileAction("DevconExe", @"remove Root\Micser.Vac.Driver", Return.check, When.Before, Step.RemoveFiles, Condition.BeingUninstalled)
                //{
                //    Execute = Execute.deferred,
                //    Impersonate = false
                //},
                //new InstalledFileAction("MicserDriverUtilityExe", @"/c 1 /s", Return.check, When.After, Step.InstallServices, Condition.NOT_Installed)
                //{
                //    Execute = Execute.deferred,
                //    Impersonate = false
                //},
            };

            project.ManagedUI.InstallDialogs
                .Add(Dialogs.Welcome)
                .Add(Dialogs.Licence)
                //.Add(Dialogs.SetupType)
                //.Add(Dialogs.Features)
                .Add(Dialogs.InstallDir)
                .Add(Dialogs.Progress)
                .Add(Dialogs.Exit);

            project.ManagedUI.ModifyDialogs
                .Add(Dialogs.MaintenanceType)
                //.Add(Dialogs.Features)
                .Add(Dialogs.Progress)
                .Add(Dialogs.Exit);

            project.BuildMsi();
        }
    }
}