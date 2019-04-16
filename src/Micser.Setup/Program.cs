using System;
using WixSharp;
using WixSharp.Forms;

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
                       !file.EndsWith(".pdb", StringComparison.InvariantCultureIgnoreCase);
            }

            var project =
                new ManagedProject(
                    "Micser",
                    new Dir(@"%ProgramFiles%\Micser",
                        new Files(@"App\*.*", FileFilter),
                        new Files(@"Driver\*.*"),
                        new File(@"Driver\Micser.Vac.Package\Micser.Vac.Driver.inf",
                            new DriverInstaller
                            {
                                Legacy = true,
                                Architecture = DriverArchitecture.x64,
                                PlugAndPlayPrompt = false
                            }
                        )
                    ),
                    new Dir(@"%ProgramMenu%\Micser",
                        new ExeFileShortcut("Micser", @"[INSTALLDIR]\App\Micser.App.exe", ""),
                        new ExeFileShortcut("Uninstall Micser", "[SystemFolder]msiexec.exe", "/x [ProductCode]")
                    )
                )
                {
                    GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b"),
                    ManagedUI = new ManagedUI()
                };

            project.ManagedUI.InstallDialogs.Add(Dialogs.Welcome)
                   //.Add(Dialogs.Licence)
                   //.Add(Dialogs.SetupType)
                   //.Add(Dialogs.Features)
                   .Add(Dialogs.InstallDir)
                   .Add(Dialogs.Progress)
                   .Add(Dialogs.Exit);

            project.ManagedUI.ModifyDialogs.Add(Dialogs.MaintenanceType)
                   //.Add(Dialogs.Features)
                   .Add(Dialogs.Progress)
                   .Add(Dialogs.Exit);

#if DEBUG
            project.SourceBaseDir = @"..\..\bin\Debug\";
#else
            project.SourceBaseDir = @"..\..\bin\Release\";
#endif

            project.BuildMsi();
        }
    }
}