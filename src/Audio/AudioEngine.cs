using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Micser.Audio;

/// <param name="Blocks">Blocks processed since the engine started.</param>
/// <param name="LateBlocks">Times the engine fell more than <see cref="AudioEngine.MaxLagBlocks"/> behind and skipped ahead.</param>
/// <param name="MaxProcessingTime">Longest time a single block took to process.</param>
public readonly record struct EngineStatistics(long Blocks, long LateBlocks, TimeSpan MaxProcessingTime);

/// <summary>
/// Processes the graph on a dedicated thread with a realtime priority where the platform allows it (MMCSS "Pro Audio" on Windows,
/// <c>SCHED_FIFO</c> on Linux), one block per block duration of the engine clock.
/// Devices run on their own clocks; their streams compensate the drift.
/// </summary>
public sealed class AudioEngine : IDisposable
{
    /// <summary>
    /// How far processing may fall behind before the engine gives up catching up and resynchronizes.
    /// </summary>
    public const int MaxLagBlocks = 4;

    private readonly AudioGraph _graph;
    private readonly ILogger _logger;
    private readonly Lock _stateLock = new();
    private long _blocks;
    private long _lateBlocks;
    private long _maxProcessingTicks;
    private volatile bool _stopRequested;
    private Thread? _thread;

    public AudioEngine(AudioGraph graph, ILogger<AudioEngine>? logger = null)
    {
        _graph = graph;
        _logger = logger ?? NullLogger<AudioEngine>.Instance;
    }

    public bool IsRunning => _thread != null;

    public EngineStatistics Statistics =>
        new(
            Interlocked.Read(ref _blocks),
            Interlocked.Read(ref _lateBlocks),
            TimeSpan.FromSeconds((double)Interlocked.Read(ref _maxProcessingTicks) / Stopwatch.Frequency)
        );

    public void Dispose()
    {
        Stop();
    }

    /// <summary>
    /// Resets <see cref="EngineStatistics.MaxProcessingTime"/>, e.g. to measure it per interval.
    /// </summary>
    public void ResetMaxProcessingTime()
    {
        Interlocked.Exchange(ref _maxProcessingTicks, 0);
    }

    public void Start()
    {
        lock (_stateLock)
        {
            if (_thread != null)
            {
                return;
            }

            _stopRequested = false;
            _thread = new Thread(Run)
            {
                Name = "Micser audio engine",
                IsBackground = true,
                Priority = ThreadPriority.Highest,
            };
            _thread.Start();
        }
    }

    public void Stop()
    {
        lock (_stateLock)
        {
            if (_thread == null)
            {
                return;
            }

            _stopRequested = true;
            _thread.Join();
            _thread = null;
            _graph.ClearLayouts();
        }
    }

    private void Run()
    {
        using var timer = BlockTimer.Create(_logger);
        var period = (long)(_graph.Format.BlockDuration.TotalSeconds * Stopwatch.Frequency);
        var deadline = Stopwatch.GetTimestamp();
        _logger.LogInformation(
            "Audio engine started ({SampleRate} Hz, {FrameCount} frames per block).",
            _graph.Format.SampleRate,
            _graph.Format.FrameCount
        );

        try
        {
            while (!_stopRequested)
            {
                var start = Stopwatch.GetTimestamp();
                _graph.Process();
                var elapsed = Stopwatch.GetTimestamp() - start;

                Interlocked.Increment(ref _blocks);
                if (elapsed > Interlocked.Read(ref _maxProcessingTicks))
                {
                    Interlocked.Exchange(ref _maxProcessingTicks, elapsed);
                }

                deadline += period;
                var now = Stopwatch.GetTimestamp();
                if (now - deadline > MaxLagBlocks * period)
                {
                    Interlocked.Increment(ref _lateBlocks);
                    deadline = now;
                }

                timer.WaitUntil(deadline);
            }
        }
        finally
        {
            _logger.LogInformation("Audio engine stopped.");
        }
    }
}
