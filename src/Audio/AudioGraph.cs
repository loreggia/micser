using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Micser.Audio;

public sealed record Connection(OutputPort Source, InputPort Target);

/// <summary>
/// The modules and connections that are processed together, one block per <see cref="Process"/> call.
/// Edits are thread-safe; once an edit returns, the next block uses the new structure.
/// </summary>
public sealed class AudioGraph
{
    private readonly List<Connection> _connections = [];
    private readonly Lock _editLock = new();
    private readonly HashSet<AudioModule> _failingModules = [];
    private readonly ILogger _logger;
    private readonly List<AudioModule> _modules = [];
    private readonly Lock _processLock = new();
    private Step[] _steps = [];

    public AudioGraph(ProcessingFormat format, ILogger<AudioGraph>? logger = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(format.SampleRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(format.FrameCount);

        Format = format;
        _logger = logger ?? NullLogger<AudioGraph>.Instance;
    }

    public IReadOnlyList<Connection> Connections
    {
        get
        {
            lock (_editLock)
            {
                return [.. _connections];
            }
        }
    }

    public ProcessingFormat Format { get; }

    public IReadOnlyList<AudioModule> Modules
    {
        get
        {
            lock (_editLock)
            {
                return [.. _modules];
            }
        }
    }

    public void Add(AudioModule module)
    {
        lock (_editLock)
        {
            module.Attach(Format);
            _modules.Add(module);
            Rebuild();
        }
    }

    /// <exception cref="InvalidOperationException">A port's module isn't part of the graph, the connection exists or it would create a cycle.</exception>
    public Connection Connect(OutputPort source, InputPort target)
    {
        lock (_editLock)
        {
            if (!_modules.Contains(source.Module) || !_modules.Contains(target.Module))
            {
                throw new InvalidOperationException("Both modules must be part of the graph.");
            }

            if (_connections.Exists(c => c.Source == source && c.Target == target))
            {
                throw new InvalidOperationException($"{source} is already connected to {target}.");
            }

            if (Reaches(target.Module, source.Module))
            {
                throw new InvalidOperationException($"Connecting {source} to {target} would create a cycle.");
            }

            var connection = new Connection(source, target);
            _connections.Add(connection);
            Rebuild();
            return connection;
        }
    }

    public bool Disconnect(OutputPort source, InputPort target)
    {
        lock (_editLock)
        {
            if (_connections.RemoveAll(c => c.Source == source && c.Target == target) == 0)
            {
                return false;
            }

            Rebuild();
            return true;
        }
    }

    /// <summary>
    /// Processes one block: every module in dependency order, each after mixing its inputs.
    /// A module that throws produces silence for that block.
    /// </summary>
    public void Process()
    {
        lock (_processLock)
        {
            foreach (var step in _steps)
            {
                try
                {
                    for (var i = 0; i < step.Inputs.Length; i++)
                    {
                        step.Inputs[i].Mix(step.Sources[i]);
                    }

                    step.Module.ProcessBlock();
                    _failingModules.Remove(step.Module);
                }
                catch (Exception ex)
                {
                    foreach (var output in step.Module.Outputs)
                    {
                        output.Buffer.SetLayout(ChannelLayout.None);
                    }

                    if (_failingModules.Add(step.Module))
                    {
                        _logger.LogError(ex, "{Module} failed to process a block.", step.Module.GetType().Name);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Removes a module and its connections. The module isn't disposed.
    /// </summary>
    public bool Remove(AudioModule module)
    {
        lock (_editLock)
        {
            if (!_modules.Remove(module))
            {
                return false;
            }

            _connections.RemoveAll(c => c.Source.Module == module || c.Target.Module == module);
            Rebuild();
            module.Detach();
            return true;
        }
    }

    private bool Reaches(AudioModule from, AudioModule to)
    {
        var visited = new HashSet<AudioModule>();
        var pending = new Stack<AudioModule>([from]);

        while (pending.TryPop(out var module))
        {
            if (module == to)
            {
                return true;
            }

            if (visited.Add(module))
            {
                foreach (var connection in _connections)
                {
                    if (connection.Source.Module == module)
                    {
                        pending.Push(connection.Target.Module);
                    }
                }
            }
        }

        return false;
    }

    private void Rebuild()
    {
        var dependencies = _modules.ToDictionary(m => m, m => _connections.Count(c => c.Target.Module == m && c.Source.Module != m));
        var ready = new Queue<AudioModule>(_modules.Where(m => dependencies[m] == 0));
        var steps = new List<Step>(_modules.Count);

        while (ready.TryDequeue(out var module))
        {
            var inputs = module.Inputs.ToArray();
            var sources = inputs
                .Select(input => _connections.Where(c => c.Target == input).Select(c => c.Source).ToArray())
                .ToArray();
            steps.Add(new Step(module, inputs, sources));

            foreach (var connection in _connections.Where(c => c.Source.Module == module))
            {
                if (--dependencies[connection.Target.Module] == 0)
                {
                    ready.Enqueue(connection.Target.Module);
                }
            }
        }

        lock (_processLock)
        {
            _steps = [.. steps];
            _failingModules.RemoveWhere(m => !_modules.Contains(m));
        }
    }

    private sealed record Step(AudioModule Module, InputPort[] Inputs, OutputPort[][] Sources);
}
