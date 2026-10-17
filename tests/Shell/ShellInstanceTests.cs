using System.Runtime.Versioning;
using Micser.Tests.Common;
using TUnit.Core.Enums;

namespace Micser.Shell.Tests;

[ExcludeOn(OS.Windows)]
[UnsupportedOSPlatform("windows")]
public class ShellInstanceTests
{
    [Test]
    public async Task TryAcquire_SecondStart_ActivatesTheFirstAndReturnsNull()
    {
        using var directory = new TempDirectory();
        using var first = ShellInstance.TryAcquire(directory.Path);
        var activated = new TaskCompletionSource();
        first!.ActivationRequested += (_, _) => activated.TrySetResult();

        var second = ShellInstance.TryAcquire(directory.Path);

        await Assert.That(second).IsNull();
        await activated.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert
            .That(File.GetUnixFileMode(directory.Path))
            .IsEqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    [Test]
    public async Task TryAcquire_AfterTheFirstExited_BecomesTheShell()
    {
        using var directory = new TempDirectory();
        ShellInstance.TryAcquire(directory.Path)!.Dispose();

        using var next = ShellInstance.TryAcquire(directory.Path);

        await Assert.That(next).IsNotNull();
    }

    [Test]
    public async Task TryAcquire_SocketOfACrashedShell_BecomesTheShell()
    {
        using var directory = TempDirectory.Create();
        await File.WriteAllTextAsync(directory.Combine("shell.sock"), "");

        using var instance = ShellInstance.TryAcquire(directory.Path);
        var activated = new TaskCompletionSource();
        instance!.ActivationRequested += (_, _) => activated.TrySetResult();

        await Assert.That(ShellInstance.TryAcquire(directory.Path)).IsNull();
        await activated.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
