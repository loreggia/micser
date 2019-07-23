using Micser.Setup.CustomActions;
using System;
using System.Linq;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;
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
                    "Micser.Engine.exe",
                    "CodeAnalysisLog.xml",
                    "lastcodeanalysissucceeded",
                    "ManualStart.ps1"
                };

                return !excludedFiles.Any(x => file.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));
            }

            var project = new Project("Micser")
            {
                GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b"),
                MajorUpgrade = MajorUpgrade.Default,
                UpgradeCode = new Guid("10C43476-AAF1-46E2-9EC8-E87DD16F9119"),
            };

#if DEBUG
            project.SourceBaseDir = @"..\..\bin\Debug\";
#else
            project.SourceBaseDir = @"..\..\bin\Release\";

            project.DigitalSignature = new DigitalSignature
            {
                PfxFilePath = @"..\..\crt\Certificate.pfx",
                Description = "Micser",
                TimeUrl = new Uri("http://timestamp.verisign.com/scripts/timstamp.dll")
            };
#endif

            project.Package.Attributes.Add("Manufacturer", "Lucas Loreggia");
            project.Package.Attributes.Add("Description", "Micser Installer");

            var appFile = System.IO.Path.GetFullPath(System.IO.Path.Combine(project.SourceBaseDir, "App", "Micser.App.exe"));
            var appAssembly = System.Reflection.Assembly.LoadFrom(appFile);
            project.Version = appAssembly.GetName().Version;

            var coreFeature = new Feature("Micser", "Installs Micser core components.", true, false)
            {
                Id = new Id("CoreFeature"),
                Attributes =
                {
                    { "AllowAdvertise","no" },
                    { "InstallDefault", "followParent" }
                }
            };
            var vacFeature = new Feature("Virtual Audio Cable", "Installs the Micser Virtual Audio Cable driver.", true, true)
            {
                Id = new Id("VacFeature"),
                Attributes =
                {
                    { "AllowAdvertise", "no" },
                    { "InstallDefault", "followParent" }
                }
            };
            project.DefaultFeature.AllowChange = false;
            project.DefaultFeature.Attributes.Add("AllowAdvertise", "no");
            project.DefaultFeature.Attributes.Add("InstallDefault", "local");
            project.DefaultFeature.Display = FeatureDisplay.expand;
            project.DefaultFeature.Children.Add(coreFeature);
            project.DefaultFeature.Children.Add(vacFeature);

            var appId = new Id("MicserAppExe");

            project.Dirs = new[]
            {
                new Dir(coreFeature, @"%ProgramFiles%\Micser",
                    new Files(coreFeature, @"App\*.*", FileFilter),
                    new File(appId, coreFeature, @"App\Micser.App.exe"),
                    new File(new Id("MicserEngineExe"), coreFeature, @"App\Micser.Engine.exe")
                    {
                        ServiceInstaller = new ServiceInstaller("Micser.Engine")
                        {
                            StartOn = SvcEvent.Install_Wait,
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
                    new ExeFileShortcut(coreFeature, "Micser", @"[INSTALLDIR]\Micser.App.exe", ""),
                    new ExeFileShortcut(coreFeature, "Uninstall Micser", "[SystemFolder]msiexec.exe", "/x [ProductCode]")
                )
            };

            var vacFeatureCondition = Condition.Create("&VacFeature = 3");

#if X64
            project.Platform = Platform.x64;
#else
            project.Platform = Platform.x86;
#endif

            var launchAppAction = new Id("LaunchApp");

            project.Actions = new Action[]
            {
                new ManagedAction(
                    DriverActions.InstallDriver,
                    typeof(DriverActions).Assembly.Location,
                    Return.check,
                    When.Before,
                    Step.InstallFinalize,
                    Condition.NOT_BeingRemoved & vacFeatureCondition)
                {
                    Rollback = nameof(DriverActions.UninstallDriver),
                    Impersonate = false,
                    Execute = Execute.deferred,
                    UsesProperties = ""
                },

                new ManagedAction(
                    DriverActions.UninstallDriver,
                    typeof(DriverActions).Assembly.Location,
                    Return.check,
                    When.After,
                    Step.InstallInitialize,
                    Condition.BeingUninstalled & vacFeatureCondition | Condition.Create("&VacFeature=2"))
                {
                    Rollback = nameof(DriverActions.InstallDriver),
                    Impersonate = false,
                    Execute = Execute.deferred,
                    UsesProperties = ""
                },

                new InstalledFileAction(launchAppAction, appId, "")
                {
                    Impersonate = true,
                    Sequence = Sequence.NotInSequence
                }
            };

            project.UI = WUI.WixUI_Common;
            project.CustomUI = new CommomDialogsUI()
                .On(NativeDialogs.WelcomeDlg, Buttons.Next, new ShowDialog(NativeDialogs.InstallDirDlg))
                .On(NativeDialogs.InstallDirDlg, Buttons.Back, new ShowDialog(NativeDialogs.WelcomeDlg))
                .On(NativeDialogs.MaintenanceTypeDlg, "ChangeButton", new ShowDialog(NativeDialogs.CustomizeDlg))
                .On(NativeDialogs.CustomizeDlg, Buttons.Back, new ShowDialog(NativeDialogs.MaintenanceTypeDlg, Condition.Installed))
                .On(NativeDialogs.ExitDialog, Buttons.Finish, new ExecuteCustomAction(launchAppAction, new Condition("WIXUI_EXITDIALOGOPTIONALCHECKBOX = 1") & Condition.NOT_Installed));
            project.CustomUI.Properties.Remove("ARPNOMODIFY");

            project.AddProperty(new Property("WIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT", "Launch Micser"));
            project.AddProperty(new Property("WIXUI_EXITDIALOGOPTIONALCHECKBOX", "1"));

            project.SetNetFxPrerequisite("WIXNETFX4RELEASEINSTALLED >= '#461808'");

            project.Include(WixExtension.UI);
            project.Include(WixExtension.NetFx);

            project.BuildMsi($"Installer\\Micser-{project.Version}-{project.Platform}.msi");
        }
    }
}