using Micser.Audio;
using Microsoft.Extensions.DependencyInjection;

namespace Micser.Engine.TestPlugin;

public sealed record TestState(float Value = 0f);

public sealed class TestModule : EffectModule, IStatefulModule<TestState>
{
    public float Value { get; set; }

    public TestState GetState()
    {
        return new TestState(Value);
    }

    public void SetState(TestState state)
    {
        Value = state.Value;
    }

    protected override void Process(AudioBuffer buffer)
    {
    }
}

/// <summary>
/// Registers <see cref="TestModule"/> as "Test". Throws after registering if a file named "throw" is in the plugin folder.
/// </summary>
public sealed class TestPlugin : IAudioPlugin
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAudioModule<TestModule, TestState>("Test");
        if (File.Exists(Path.Combine(Path.GetDirectoryName(typeof(TestPlugin).Assembly.Location)!, "throw")))
        {
            throw new InvalidOperationException("The test plugin failed.");
        }
    }
}
