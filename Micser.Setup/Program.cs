﻿using System;
using WixSharp;
using WixSharp.Forms;

namespace Micser.Setup
{
    internal class Program
    {
        private static void Main()
        {
            var project =
                new ManagedProject("Micser",
                    new Dir(@"%ProgramFiles%\Micser",
                        new Dir(@"App",
                            new Files(@"App\*.*",
                                file => !file.EndsWith(".test.dll", StringComparison.InvariantCultureIgnoreCase) &&
                                        !file.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase) &&
                                        !file.EndsWith(".pdb", StringComparison.InvariantCultureIgnoreCase))),
                        new Dir(@"Driver",
                            new Files(@"Driver\Micser.Vac.Package\*.*"))),
                    new Dir(@"%ProgramMenu%\Micser",
                        new ExeFileShortcut("Micser",
                            @"[INSTALLDIR]\App\Micser.Core.exe", ""),
                        new ExeFileShortcut("Uninstall Micser", "[System64Folder]msiexec.exe", "/x [ProductCode]")))
                {
                    GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b"),
                    ManagedUI = new ManagedUI()
                };

            project.ManagedUI.InstallDialogs.Add(Dialogs.Welcome)
                                            .Add(Dialogs.Licence)
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
            project.SourceBaseDir = @"..\Bin\Debug\";
#else
            project.SourceBaseDir = @"..\Bin\Release\";
#endif

            project.BuildMsi();
        }
    }
}