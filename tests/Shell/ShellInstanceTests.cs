using System.Runtime.Versioning;
using TUnit.Core.Enums;

namespace Micser.Shell.Tests;

[ExcludeOn(OS.Windows)]
[UnsupportedOSPlatform("windows")]
public class ShellInstanceTests
{
    [Test]
    public async Task TryAcquire_SecondStart_ActivatesTheFirstAndReturnsNull()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            using var first = ShellInstance.TryAcquire(directory);
            var activated = new TaskCompletionSource();
            first!.ActivationRequested += (_, _) => activated.TrySetResult();

            var second = ShellInstance.TryAcquire(directory);

            await Assert.That(second).IsNull();
            await activated.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Assert
                .That(File.GetUnixFileMode(directory))
                .IsEqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task TryAcquire_AfterTheFirstExited_BecomesTheShell()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            ShellInstance.TryAcquire(directory)!.Dispose();

            using var next = ShellInstance.TryAcquire(directory);

            await Assert.That(next).IsNotNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task TryAcquire_SocketOfACrashedShell_BecomesTheShell()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "shell.sock"), "");

            using var instance = ShellInstance.TryAcquire(directory);
            var activated = new TaskCompletionSource();
            instance!.ActivationRequested += (_, _) => activated.TrySetResult();

            await Assert.That(ShellInstance.TryAcquire(directory)).IsNull();
            await activated.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
