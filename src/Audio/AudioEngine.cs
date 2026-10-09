using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32.SafeHandles;

namespace Micser.Audio;

/// <param name="Blocks">Blocks processed since the engine started.</param>
/// <param name="LateBlocks">Times the engine fell more than <see cref="AudioEngine.MaxLagBlocks"/> behind and skipped ahead.</param>
/// <param name="MaxProcessingTime">Longest time a single block took to process.</param>
public readonly record struct EngineStatistics(long Blocks, long LateBlocks, TimeSpan MaxProcessingTime);

/// <summary>
/// Processes the graph on a dedicated MMCSS "Pro Audio" thread, one block per block duration of the engine clock.
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

    public EngineStatistics Statistics => new(
        Interlocked.Read(ref _blocks),
        Interlocked.Read(ref _lateBlocks),
        TimeSpan.FromSeconds((double)Interlocked.Read(ref _maxProcessingTicks) / Stopwatch.Frequency));

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

    private static void WaitUntil(SafeWaitHandle timer, long deadline)
    {
        var remaining = deadline - Stopwatch.GetTimestamp();
        if (remaining <= 0)
        {
            return;
        }

        if (timer.IsInvalid)
        {
            Thread.Sleep(TimeSpan.FromSeconds((double)remaining / Stopwatch.Frequency));
            return;
        }

        // negative due time = relative, in 100 ns units
        var dueTime = -(long)((double)remaining / Stopwatch.Frequency * 10_000_000);
        if (NativeMethods.SetWaitableTimer(timer, dueTime, 0, 0, 0, false))
        {
            NativeMethods.WaitForSingleObject(timer, NativeMethods.Infinite);
        }
    }

    private void Run()
    {
        uint taskIndex = 0;
        var mmcssHandle = NativeMethods.AvSetMmThreadCharacteristics("Pro Audio", ref taskIndex);
        if (mmcssHandle == 0)
        {
            _logger.LogWarning("Could not register the audio thread with MMCSS.");
        }

        using var timer = NativeMethods.CreateWaitableTimerEx(0, null, NativeMethods.CreateWaitableTimerHighResolution, NativeMethods.TimerAllAccess);
        if (timer.IsInvalid)
        {
            _logger.LogWarning("High resolution timers are not available; block timing will be less precise.");
        }

        var period = (long)(_graph.Format.BlockDuration.TotalSeconds * Stopwatch.Frequency);
        var deadline = Stopwatch.GetTimestamp();
        _logger.LogInformation("Audio engine started ({SampleRate} Hz, {FrameCount} frames per block).", _graph.Format.SampleRate, _graph.Format.FrameCount);

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

                WaitUntil(timer, deadline);
            }
        }
        finally
        {
            if (mmcssHandle != 0)
            {
                NativeMethods.AvRevertMmThreadCharacteristics(mmcssHandle);
            }

            _logger.LogInformation("Audio engine stopped.");
        }
    }
}
