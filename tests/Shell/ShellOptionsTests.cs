namespace Micser.Shell.Tests;

public class ShellOptionsTests
{
    [Test]
    public async Task Parse_AllArguments()
    {
        var options = ShellOptions.Parse(["--engine", @"C:\engine\Micser.Engine.exe", "--ui", "http://localhost:5173", "--minimized"]);

        await Assert.That(options).IsEqualTo(new ShellOptions(@"C:\engine\Micser.Engine.exe", new Uri("http://localhost:5173"), true));
    }

    [Test]
    public async Task Parse_NoArguments_UsesDefaults()
    {
        await Assert.That(ShellOptions.Parse([])).IsEqualTo(new ShellOptions(null, null, false));
    }

    [Test]
    [Arguments("--engine")]
    [Arguments("--unknown")]
    public async Task Parse_InvalidArguments_Throws(string argument)
    {
        await Assert.That(() => ShellOptions.Parse([argument])).Throws<ArgumentException>();
    }
}
