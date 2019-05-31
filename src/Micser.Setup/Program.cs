using Micser.Setup.CustomActions;
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
                    "Micser.Engine.exe",
                    "CodeAnalysisLog.xml",
                    "lastcodeanalysissucceeded",
                    "ManualStart.ps1"
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

            var coreFeature = new Feature("Micser", "Installs Micser core components.", true, false) { Id = new Id("CoreFeature") };
            var vacFeature = new Feature("Virtual Audio Cable", "Installs the Micser Virtual Audio Cable driver.", true, true) { Id = new Id("VacFeature") };

            project.Dirs = new[]
            {
                new Dir(coreFeature, @"%ProgramFiles%\Micser",
                    new Files(coreFeature, @"App\*.*", FileFilter),
                    new File(new Id("MicserEngineExe"), coreFeature, @"App\Micser.Engine.exe")
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
                            Interactive = false,
                            FirstFailureActionType = FailureActionType.restart,
                            SecondFailureActionType = FailureActionType.restart,
                            ThirdFailureActionType = FailureActionType.restart
                        }
                    },
                    new Dir(vacFeature, @"Driver",
                        new Files(vacFeature, @"Driver\Micser.Vac.Package\*.*")
                    )
                ),
                new Dir(coreFeature, @"%ProgramMenu%\Micser",
                    new ExeFileShortcut("Micser", @"[INSTALLDIR]\Micser.App.exe", ""),
                    new ExeFileShortcut("Uninstall Micser", "[SystemFolder]msiexec.exe", "/x [ProductCode]")
                )
            };

            var vacFeatureCondition = Condition.Create("&VacFeature=3");

            project.Actions = new Action[]
            {
                new ManagedAction(
                    DriverActions.Install,
                    typeof(DriverActions).Assembly.Location,
                    Return.check,
                    When.After,
                    Step.InstallFinalize,
                    Condition.NOT_BeingRemoved & vacFeatureCondition)
                {
                    Rollback = nameof(DriverActions.Uninstall)
                },

                new ManagedAction(
                    DriverActions.Uninstall,
                    typeof(DriverActions).Assembly.Location,
                    Return.check,
                    When.Before,
                    Step.InstallInitialize,
                    Condition.BeingUninstalled & vacFeatureCondition | Condition.Create("&VacFeature=2"))
                {
                    Rollback = nameof(DriverActions.Install)
                }
            };

            project.ManagedUI.InstallDialogs
                .Add(Dialogs.Welcome)
                .Add(Dialogs.Licence)
                //.Add(Dialogs.SetupType)
                .Add(Dialogs.Features)
                .Add(Dialogs.InstallDir)
                .Add(Dialogs.Progress)
                .Add(Dialogs.Exit);

            project.ManagedUI.ModifyDialogs
                .Add(Dialogs.MaintenanceType)
                .Add(Dialogs.Features)
                .Add(Dialogs.Progress)
                .Add(Dialogs.Exit);

            project.DefaultFeature.Display = FeatureDisplay.expand;
            project.DefaultFeature.Children.Add(coreFeature);
            project.DefaultFeature.Children.Add(vacFeature);

            project.BuildMsi();
        }
    }
}