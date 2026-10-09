using System.IO.Compression;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Micser.Audio;
using Micser.Engine.Plugins;

namespace Micser.Engine.Tests;

public sealed class PluginLoaderTests : IDisposable
{
    private static readonly string TestPluginOutput = Path.Combine(AppContext.BaseDirectory, "TestPlugin");

    private readonly string _builtInPath;
    private readonly string _directory = EngineFactory.CreateTemporaryDirectory();
    private readonly string _userPath;

    public PluginLoaderTests()
    {
        _builtInPath = Path.Combine(_directory, "builtin");
        _userPath = Path.Combine(_directory, "user");
        Directory.CreateDirectory(_builtInPath);
        Directory.CreateDirectory(_userPath);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (UnauthorizedAccessException)
        {
            // loaded plugin assemblies stay locked until the process exits
        }
    }

    [Test]
    public async Task Load_DuplicateId_LoadsTheBuiltInPlugin()
    {
        CopyTestPlugin(_builtInPath);
        CopyTestPlugin(_userPath);
        var services = new ServiceCollection();

        var catalog = PluginLoader.Load(services, _builtInPath, _userPath);

        await Assert.That(catalog.Plugins).Count().IsEqualTo(2);
        await Assert.That(catalog.Plugins[0].IsLoaded).IsTrue();
        await Assert.That(catalog.Plugins[0].IsBuiltIn).IsTrue();
        await Assert.That(catalog.Plugins[1].Error).Contains("already loaded");
        await Assert.That(services.Count(d => d.ImplementationInstance is AudioModuleDefinition)).IsEqualTo(1);
    }

    [Test]
    public async Task Load_FolderNotNamedById_Fails()
    {
        CopyTestPlugin(_userPath, "Other");

        var catalog = PluginLoader.Load(new ServiceCollection(), _builtInPath, _userPath);

        await Assert.That(catalog.Plugins.Single().Error).Contains("named by its id");
    }

    [Test]
    public async Task Load_InvalidManifest_Fails()
    {
        var directory = CopyTestPlugin(_userPath);
        await File.WriteAllTextAsync(
            Path.Combine(directory, PluginManifest.FileName),
            """{ "id": "Test", "name": "Test", "version": "1", "assembly": "..\\x.dll" }"""
        );

        var catalog = PluginLoader.Load(new ServiceCollection(), _builtInPath, _userPath);

        await Assert.That(catalog.Plugins.Single().IsLoaded).IsFalse();
        await Assert.That(catalog.Plugins.Single().Id).IsEqualTo("Test");
    }

    [Test]
    public async Task Load_RegistersModulesInTheirOwnLoadContext()
    {
        CopyTestPlugin(_userPath);
        var services = new ServiceCollection();

        var catalog = PluginLoader.Load(services, _builtInPath, _userPath);

        var plugin = catalog.Plugins.Single();
        await Assert.That(plugin.IsLoaded).IsTrue();
        await Assert.That(plugin.IsBuiltIn).IsFalse();
        await Assert.That(plugin.Manifest!.Version).IsEqualTo("1.2.3");

        var definition = services.Select(d => d.ImplementationInstance).OfType<AudioModuleDefinition>().Single();
        await Assert.That(definition.Type).IsEqualTo("Test");
        await Assert
            .That(AssemblyLoadContext.GetLoadContext(definition.ModuleType.Assembly))
            .IsNotEqualTo(AssemblyLoadContext.Default);

        // Micser.Audio is shared with the engine, so the module is an AudioModule of the host
        await Assert.That(definition.CreateModule(services.BuildServiceProvider())).IsAssignableTo<AudioModule>();
    }

    [Test]
    public async Task Load_ThrowingPlugin_FailsAndRegistersNothing()
    {
        var directory = CopyTestPlugin(_userPath);
        await File.WriteAllTextAsync(Path.Combine(directory, "throw"), "");
        var services = new ServiceCollection();

        var catalog = PluginLoader.Load(services, _builtInPath, _userPath);

        await Assert.That(catalog.Plugins.Single().Error).IsEqualTo("The test plugin failed.");
        await Assert.That(services).IsEmpty();
    }

    [Test]
    public async Task StageInstall_IsAppliedAtTheNextLoad()
    {
        var installer = new PluginInstaller(
            _userPath,
            PluginLoader.Load(new ServiceCollection(), _builtInPath, _userPath)
        );

        using (var package = CreatePackage(CopyTestPlugin(_directory)))
        {
            var manifest = installer.StageInstall(package);
            await Assert.That(manifest.Id).IsEqualTo("Test");
        }

        await Assert.That(installer.GetPendingChange("Test")).IsEqualTo(PluginChange.Install);
        await Assert.That(installer.GetPendingInstalls().Select(m => m.Id)).IsEquivalentTo(["Test"]);
        await Assert.That(Directory.Exists(Path.Combine(_userPath, "Test"))).IsFalse();

        var catalog = PluginLoader.Load(new ServiceCollection(), _builtInPath, _userPath);

        await Assert.That(catalog.Plugins.Single().IsLoaded).IsTrue();
        await Assert
            .That(new PluginInstaller(_userPath, catalog).GetPendingChange("Test"))
            .IsEqualTo(PluginChange.None);
    }

    [Test]
    public async Task StageInstall_RejectsBuiltInIds()
    {
        CopyTestPlugin(_builtInPath);
        var installer = new PluginInstaller(
            _userPath,
            PluginLoader.Load(new ServiceCollection(), _builtInPath, _userPath)
        );
        using var package = CreatePackage(CopyTestPlugin(_directory));

        await Assert.That(() => installer.StageInstall(package)).Throws<InvalidDataException>();
        await Assert.That(installer.GetPendingInstalls()).IsEmpty();
    }

    [Test]
    public async Task StageInstall_RejectsInvalidPackages()
    {
        var installer = new PluginInstaller(
            _userPath,
            PluginLoader.Load(new ServiceCollection(), _builtInPath, _userPath)
        );

        using var notZip = new MemoryStream([1, 2, 3]);
        using var withoutManifest = CreatePackage(("readme.txt", "hello"));
        using var outsideFolder = CreatePackage((PluginManifest.FileName, "{}"), ("../evil.txt", "evil"));

        await Assert.That(() => installer.StageInstall(notZip)).Throws<InvalidDataException>();
        await Assert.That(() => installer.StageInstall(withoutManifest)).Throws<InvalidDataException>();
        await Assert.That(() => installer.StageInstall(outsideFolder)).Throws<InvalidDataException>();
        await Assert.That(File.Exists(Path.Combine(_userPath, "evil.txt"))).IsFalse();
        await Assert.That(Directory.GetFileSystemEntries(Path.Combine(_userPath, ".pending"))).IsEmpty();
    }

    [Test]
    public async Task StageRemoval_IsAppliedAtTheNextLoad()
    {
        CopyTestPlugin(_userPath);

        // not loaded, as after an engine restart, so its files aren't in use
        var installer = new PluginInstaller(_userPath, new PluginCatalog([], []));

        await Assert.That(installer.StageRemoval("Test")).IsTrue();
        await Assert.That(installer.StageRemoval("Missing")).IsFalse();
        await Assert.That(installer.GetPendingChange("Test")).IsEqualTo(PluginChange.Remove);

        var catalog = PluginLoader.Load(new ServiceCollection(), _builtInPath, _userPath);

        await Assert.That(catalog.Plugins).IsEmpty();
        await Assert.That(Directory.Exists(Path.Combine(_userPath, "Test"))).IsFalse();
    }

    [Test]
    public async Task StageRemoval_RejectsBuiltInPlugins()
    {
        CopyTestPlugin(_builtInPath);
        var installer = new PluginInstaller(
            _userPath,
            PluginLoader.Load(new ServiceCollection(), _builtInPath, _userPath)
        );

        await Assert.That(() => installer.StageRemoval("Test")).Throws<InvalidDataException>();
    }

    internal static string CopyTestPlugin(string root, string folderName = "Test")
    {
        var target = Path.Combine(root, folderName);
        foreach (var file in Directory.EnumerateFiles(TestPluginOutput, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(TestPluginOutput, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }

        return target;
    }

    internal static MemoryStream CreatePackage(string directory)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                archive.CreateEntryFromFile(file, Path.GetFileName(file));
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreatePackage(params (string Name, string Content)[] entries)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var writer = new StreamWriter(archive.CreateEntry(name).Open());
                writer.Write(content);
            }
        }

        stream.Position = 0;
        return stream;
    }
}
