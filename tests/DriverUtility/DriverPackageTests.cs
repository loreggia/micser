namespace Micser.DriverUtility.Tests;

public class DriverPackageTests
{
    [Test]
    public async Task Load_FolderWithInf_ReadsVersion()
    {
        var directory = Directory.CreateTempSubdirectory("micser-driver-");
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory.FullName, "Micser.Vac.inf"), "[Version]\r\nDriverVer = 10/03/2026,1.2.3.4\r\n");

            var package = DriverPackage.Load(directory.FullName);

            await Assert.That(package?.Version).IsEqualTo(new Version(1, 2, 3, 4));
            await Assert.That(package?.InfPath).IsEqualTo(Path.Combine(directory.FullName, "Micser.Vac.inf"));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Test]
    public async Task Load_FolderWithoutInf_ReturnsNull()
    {
        var directory = Directory.CreateTempSubdirectory("micser-driver-");
        try
        {
            await Assert.That(DriverPackage.Load(directory.FullName)).IsNull();
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Test]
    [Arguments("DriverVer = 10/03/2026,1.0.0.0", "1.0.0.0")]
    [Arguments("  driverver=01/01/2026, 2.5.10.0 ; comment", "2.5.10.0")]
    [Arguments("[Version]\nSignature = \"$Windows NT$\"\nDriverVer   = 01/01/2026,3.1", "3.1")]
    public async Task ParseDriverVersion_ReadsVersion(string infText, string expected)
    {
        await Assert.That(DriverPackage.ParseDriverVersion(infText)).IsEqualTo(Version.Parse(expected));
    }

    [Test]
    [Arguments("")]
    [Arguments("; DriverVer = 01/01/2026,1.0.0.0")]
    [Arguments("DriverVer = 01/01/2026")]
    public async Task ParseDriverVersion_NoVersion_ReturnsNull(string infText)
    {
        await Assert.That(DriverPackage.ParseDriverVersion(infText)).IsNull();
    }
}
