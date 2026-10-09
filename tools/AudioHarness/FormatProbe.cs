using System.Diagnostics;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Micser.AudioHarness;

/// <summary>
/// Checks which stream formats a render and a capture endpoint accept, in shared mode with and without the audio engine's format conversion
/// and in exclusive mode, and sends a 1 kHz tone from the render to the capture endpoint (e.g. through a virtual cable) in a few format pairs.
/// Then it plays a different tone per channel and shows where each one arrives, between the mix formats and stereo, 5.1 and the mix formats.
/// Finally it plays a counting sequence in exclusive mode at 16, 24 and 32 bits and checks that it arrives bit-exact.
/// </summary>
internal static class FormatProbe
{
    private const double ToneAmplitude = 0.1;
    private const double ToneFrequency = 1000;

    private static readonly Guid IeeeFloatSubFormat = new("00000003-0000-0010-8000-00aa00389b71");

    private static readonly AudioClientStreamFlags ConvertFlags =
        AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality;

    private static readonly WaveFormatExtensible[] Formats =
    [
        Pcm(48000, 32, 2),
        Float(48000, 2),
        Pcm(48000, 24, 2),
        Pcm(48000, 32, 2, 24),
        Pcm(48000, 16, 2),
        Pcm(44100, 16, 2),
        Pcm(44100, 24, 2),
        Pcm(96000, 24, 2),
        Pcm(48000, 16, 1),
        Pcm(16000, 16, 1),
        Float(48000, 6),
    ];

    public static void Run(string renderId, string captureId)
    {
        using var enumerator = new MMDeviceEnumerator();
        using var render = enumerator.GetDevice(renderId);
        using var capture = enumerator.GetDevice(captureId);

        PrintSupport(render);
        PrintSupport(capture);

        WaveFormat renderMix;
        WaveFormat captureMix;
        using (var client = render.CreateAudioClient())
        {
            renderMix = client.MixFormat;
        }

        using (var client = capture.CreateAudioClient())
        {
            captureMix = client.MixFormat;
        }

        // exclusive streams use the endpoints' channels and speaker mask
        var channels = renderMix.Channels;
        var mask = renderMix is WaveFormatExtensible { ChannelMask: var mixMask } ? mixMask : Mask(channels);
        WaveFormatExtensible Exclusive(int bits, int? validBits = null) => Pcm(48000, bits, channels, validBits, mask);
        const AudioClientShareMode shared = AudioClientShareMode.Shared;
        const AudioClientShareMode exclusive = AudioClientShareMode.Exclusive;

        Console.WriteLine(
            "Tone through the endpoints (1 kHz at -20 dBFS peak, -23 dBFS RMS; level and tone share of channel 1 after 0.5 s):"
        );
        Transfer(render, Pcm(44100, 16, 1), shared, capture, Pcm(16000, 16, 1), shared);
        Transfer(render, Float(48000, 6), shared, capture, Float(48000, 2), shared);
        Transfer(render, Pcm(44100, 24, 2), shared, capture, Pcm(96000, 24, 2), shared);
        Transfer(render, Exclusive(32), exclusive, capture, Exclusive(32), exclusive);
        Transfer(render, Exclusive(32), exclusive, capture, Pcm(16000, 16, 1), shared);
        Transfer(render, Exclusive(16), exclusive, capture, Exclusive(24), exclusive);
        Transfer(render, Exclusive(24), exclusive, capture, Float(48000, 2), shared);
        Transfer(render, Float(48000, 2), shared, capture, Exclusive(16), exclusive);
        Transfer(render, Exclusive(32, 24), exclusive, capture, Pcm(48000, 16, 2), shared);
        Transfer(render, Pcm(44100, 16, channels, null, mask), exclusive, capture, Float(48000, 2), shared);
        Console.WriteLine();

        (WaveFormat Render, WaveFormat Capture)[] maps =
        [
            (renderMix, captureMix),
            (Float(48000, 2), Float(48000, 2)),
            (Float(48000, 6), captureMix),
            (Float(48000, 2), captureMix),
            (renderMix, Float(48000, 2)),
        ];

        // on a stereo endpoint several of them are the same
        foreach (var (renderFormat, captureFormat) in maps.DistinctBy(m => (Describe(m.Render), Describe(m.Capture))))
        {
            ChannelMap(render, renderFormat, capture, captureFormat);
        }

        Console.WriteLine("Counting sequence in exclusive mode (all channels, after the initial silence):");
        BitExact(render, Exclusive(16), capture, Exclusive(16));
        BitExact(render, Exclusive(24), capture, Exclusive(24));
        BitExact(render, Exclusive(32, 24), capture, Exclusive(24));
        BitExact(render, Exclusive(16), capture, Exclusive(32));
    }

    /// <summary>
    /// Plays a counting sequence at the render format's valid bits on every channel and checks that each captured frame holds the next
    /// value, compared at the smaller of the two formats' valid bits (a conversion to more bits keeps every value).
    /// </summary>
    private static void BitExact(
        MMDevice render,
        WaveFormatExtensible renderFormat,
        MMDevice capture,
        WaveFormatExtensible captureFormat
    )
    {
        var bits = Math.Min(renderFormat.ValidBitsPerSample, captureFormat.ValidBitsPerSample);
        var label = $"  {Describe(renderFormat)} -> {Describe(captureFormat)}";
        double Value(long frame, int channel) =>
            ((frame % (1L << bits)) - (1L << (bits - 1))) / (double)(1L << (bits - 1));

        List<double>[] captured;
        try
        {
            using var renderClient = Initialize(
                render,
                renderFormat,
                AudioClientShareMode.Exclusive,
                AudioClientStreamFlags.None
            );
            using var captureClient = Initialize(
                capture,
                captureFormat,
                AudioClientShareMode.Exclusive,
                AudioClientStreamFlags.None
            );
            captured = Stream(renderClient, renderFormat, captureClient, captureFormat, Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{label}: fails ({ErrorName(ex)})");
            return;
        }

        var scale = (double)(1L << (bits - 1));
        var frames = captured[0].Count;
        var start = captured[0].FindIndex(v => v != 0);
        if (start < 0)
        {
            Console.WriteLine($"{label}: nothing captured");
            return;
        }

        // the first frame after the silence may be the sequence's zero crossing, so start one later
        var checkedFrames = 0;
        var errors = 0;
        for (var frame = start + 1; frame < frames - 1; frame++)
        {
            var expected = (long)Math.Round(captured[0][frame] * scale) + 1;
            expected = expected >= (1L << (bits - 1)) ? -(1L << (bits - 1)) : expected;
            for (var channel = 0; channel < captured.Length; channel++)
            {
                if ((long)Math.Round(captured[channel][frame + 1] * scale) != expected)
                {
                    errors++;
                    break;
                }
            }

            checkedFrames++;
        }

        Console.WriteLine($"{label}: {checkedFrames} frames compared at {bits} bits, {errors} not the next value");
    }

    /// <summary>
    /// Plays a different tone on each channel (500 Hz on channel 1, 700 Hz on channel 2, ...) and shows, per captured channel, the level of
    /// each tone.
    /// </summary>
    private static void ChannelMap(MMDevice render, WaveFormat renderFormat, MMDevice capture, WaveFormat captureFormat)
    {
        static double Frequency(int channel) => 500 + 200 * channel;
        double Sample(long frame, int channel) =>
            ToneAmplitude * Math.Sin(2 * Math.PI * Frequency(channel) * frame / renderFormat.SampleRate);

        Console.WriteLine(
            $"Channel map, {Describe(renderFormat)} shared -> {Describe(captureFormat)} shared (dBFS RMS of each render channel's tone; -23 = unchanged):"
        );
        using var renderClient = Initialize(render, renderFormat, AudioClientShareMode.Shared, ConvertFlags);
        using var captureClient = Initialize(capture, captureFormat, AudioClientShareMode.Shared, ConvertFlags);
        var captured = Stream(renderClient, renderFormat, captureClient, captureFormat, Sample);

        Console.WriteLine(
            "  "
                + "captured".PadRight(10)
                + string.Concat(
                    Enumerable
                        .Range(0, renderFormat.Channels)
                        .Select(c => $"out {c + 1} ({Frequency(c)} Hz)".PadLeft(18))
                )
        );
        for (var channel = 0; channel < captureFormat.Channels; channel++)
        {
            var samples = captured[channel].Skip(captureFormat.SampleRate / 2).ToArray();
            var levels = Enumerable
                .Range(0, renderFormat.Channels)
                .Select(c =>
                {
                    var power = samples.Length > 0 ? TonePower(samples, captureFormat.SampleRate, Frequency(c)) : 0;
                    return power > 1e-9 ? $"{10 * Math.Log10(power), 18:0.0}" : "-".PadLeft(18);
                });
            Console.WriteLine("  " + $"in {channel + 1}".PadRight(10) + string.Concat(levels));
        }

        Console.WriteLine();
    }

    private static string Describe(WaveFormat format)
    {
        var bits =
            format is WaveFormatExtensible { ValidBitsPerSample: var valid } && valid != format.BitsPerSample
                ? $"{valid}-in-{format.BitsPerSample}-bit"
                : $"{format.BitsPerSample}-bit";
        return $"{format.SampleRate} Hz {format.Channels} ch {bits} {(IsFloat(format) ? "float" : "int")}";
    }

    private static string ErrorName(Exception exception)
    {
        return (uint)exception.HResult switch
        {
            0x88890008 => "UNSUPPORTED_FORMAT",
            0x8889000A => "DEVICE_IN_USE",
            0x8889000E => "EXCLUSIVE_MODE_NOT_ALLOWED",
            0x88890019 => "BUFFER_SIZE_NOT_ALIGNED",
            0x80070057 => "E_INVALIDARG",
            var code => $"0x{code:X8}",
        };
    }

    private static WaveFormatExtensible Float(int rate, int channels)
    {
        return new WaveFormatExtensible(rate, 32, channels, true, 32, Mask(channels));
    }

    /// <summary>
    /// Opens a stream; exclusive streams use a 100 ms buffer, realigned if the driver asks for it.
    /// </summary>
    private static AudioClient Initialize(
        MMDevice device,
        WaveFormat format,
        AudioClientShareMode mode,
        AudioClientStreamFlags flags
    )
    {
        var client = device.CreateAudioClient();
        try
        {
            if (mode == AudioClientShareMode.Shared)
            {
                client.Initialize(mode, flags, 1_000_000, 0, format, Guid.Empty);
                return client;
            }

            // the harness polls, so exclusive streams get more than one device period of buffer
            var period = client.DefaultDevicePeriod;
            const long buffer = 1_000_000;
            try
            {
                client.Initialize(mode, flags, buffer, period, format, Guid.Empty);
                return client;
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x88890019)
            {
                var frames = client.BufferSize;
                client.Dispose();
                client = device.CreateAudioClient();
                var aligned = (long)(10_000_000.0 * frames / format.SampleRate + 0.5);
                client.Initialize(mode, flags, aligned, period, format, Guid.Empty);
                return client;
            }
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static bool IsFloat(WaveFormat format)
    {
        return format is WaveFormatExtensible extensible
            ? extensible.SubFormat == IeeeFloatSubFormat
            : format.Encoding == WaveFormatEncoding.IeeeFloat;
    }

    private static int Mask(int channels)
    {
        return channels switch
        {
            1 => 0x4,
            2 => 0x3,
            6 => 0x3F,
            _ => 0,
        };
    }

    private static WaveFormatExtensible Pcm(int rate, int bits, int channels, int? validBits = null, int? mask = null)
    {
        return new WaveFormatExtensible(rate, bits, channels, false, validBits ?? bits, mask ?? Mask(channels));
    }

    private static void PrintSupport(MMDevice device)
    {
        using (var client = device.CreateAudioClient())
        {
            Console.WriteLine($"{device.FriendlyName} ({device.DataFlow}), mix format {Describe(client.MixFormat)}");
        }

        Console.WriteLine($"  {"format", -28} {"shared", -20} {"shared + convert", -20} exclusive");
        foreach (var format in Formats)
        {
            Console.WriteLine(
                $"  {Describe(format), -28} {Try(device, format, AudioClientShareMode.Shared, AudioClientStreamFlags.None), -20} "
                    + $"{Try(device, format, AudioClientShareMode.Shared, ConvertFlags), -20} {Try(device, format, AudioClientShareMode.Exclusive, AudioClientStreamFlags.None)}"
            );
        }

        Console.WriteLine();
    }

    private static double ReadSample(byte[] data, int offset, WaveFormat format)
    {
        var isFloat = IsFloat(format);
        return format.BitsPerSample switch
        {
            16 => BitConverter.ToInt16(data, offset) / 32768.0,
            24 => ((data[offset] << 8) | (data[offset + 1] << 16) | (data[offset + 2] << 24)) / 2147483648.0,
            32 when isFloat => BitConverter.ToSingle(data, offset),
            32 => BitConverter.ToInt32(data, offset) / 2147483648.0,
            _ => throw new NotSupportedException(),
        };
    }

    /// <summary>
    /// Plays the tone into <paramref name="render"/> for 2 s and reports what arrives at <paramref name="capture"/>.
    /// </summary>
    private static void Transfer(
        MMDevice render,
        WaveFormat renderFormat,
        AudioClientShareMode renderMode,
        MMDevice capture,
        WaveFormat captureFormat,
        AudioClientShareMode captureMode
    )
    {
        var label =
            $"  {Describe(renderFormat)} {renderMode.ToString().ToLowerInvariant()} -> {Describe(captureFormat)} {captureMode.ToString().ToLowerInvariant()}";
        AudioClient renderClient;
        AudioClient captureClient;
        try
        {
            renderClient = Initialize(
                render,
                renderFormat,
                renderMode,
                renderMode == AudioClientShareMode.Shared ? ConvertFlags : AudioClientStreamFlags.None
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{label}: render fails ({ErrorName(ex)})");
            return;
        }

        using (renderClient)
        {
            try
            {
                captureClient = Initialize(
                    capture,
                    captureFormat,
                    captureMode,
                    captureMode == AudioClientShareMode.Shared ? ConvertFlags : AudioClientStreamFlags.None
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{label}: capture fails ({ErrorName(ex)})");
                return;
            }

            using (captureClient)
            {
                var captured = Stream(
                    renderClient,
                    renderFormat,
                    captureClient,
                    captureFormat,
                    (frame, _) =>
                        ToneAmplitude * Math.Sin(2 * Math.PI * ToneFrequency * frame / renderFormat.SampleRate)
                )[0];
                var skip = captureFormat.SampleRate / 2;
                if (captured.Count <= skip)
                {
                    Console.WriteLine($"{label}: nothing captured");
                    return;
                }

                var samples = captured.Skip(skip).ToArray();
                var power = samples.Average(s => s * s);
                var rmsDb = power > 0 ? 10 * Math.Log10(power) : double.NegativeInfinity;
                var tonePower = TonePower(samples, captureFormat.SampleRate, ToneFrequency);
                var crossings = samples.Skip(1).Where((sample, i) => samples[i] < 0 && sample >= 0).Count();
                var frequency = crossings * (double)captureFormat.SampleRate / samples.Length;
                Console.WriteLine(
                    $"{label}: level {rmsDb, 6:0.0} dBFS RMS, tone {(power > 0 ? tonePower / power * 100 : 0), 5:0.0}% of the signal, "
                        + $"{frequency:0} Hz by zero crossings ({captured.Count} frames captured)"
                );
            }
        }
    }

    /// <param name="sample">The value of a channel in a frame, from -1 to just below 1.</param>
    private static List<double>[] Stream(
        AudioClient renderClient,
        WaveFormat renderFormat,
        AudioClient captureClient,
        WaveFormat captureFormat,
        Func<long, int, double> sample
    )
    {
        var renderOut = renderClient.AudioRenderClient;
        var captureIn = captureClient.AudioCaptureClient;
        var renderBufferFrames = renderClient.BufferSize;
        var captured = Enumerable.Range(0, captureFormat.Channels).Select(_ => new List<double>()).ToArray();
        long renderedFrames = 0;

        // Sleep(2) takes about 15 ms at the default timer resolution
        TimeBeginPeriod(1);
        renderClient.Start();
        captureClient.Start();
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(2))
        {
            var free = renderBufferFrames - renderClient.CurrentPadding;
            if (free > 0)
            {
                var bytes = new byte[free * renderFormat.BlockAlign];
                for (var frame = 0; frame < free; frame++)
                {
                    for (var channel = 0; channel < renderFormat.Channels; channel++)
                    {
                        var value = sample(renderedFrames + frame, channel);
                        WriteSample(
                            bytes,
                            frame * renderFormat.BlockAlign + channel * renderFormat.BitsPerSample / 8,
                            renderFormat,
                            value
                        );
                    }
                }

                Marshal.Copy(bytes, 0, renderOut.GetBuffer(free), bytes.Length);
                renderOut.ReleaseBuffer(free, AudioClientBufferFlags.None);
                renderedFrames += free;
            }

            while (captureIn.GetNextPacketSize() > 0)
            {
                var pointer = captureIn.GetBuffer(out var frames, out var flags);
                var bytes = new byte[frames * captureFormat.BlockAlign];
                Marshal.Copy(pointer, bytes, 0, bytes.Length);
                captureIn.ReleaseBuffer(frames);
                for (var frame = 0; frame < frames; frame++)
                {
                    for (var channel = 0; channel < captureFormat.Channels; channel++)
                    {
                        var offset = frame * captureFormat.BlockAlign + channel * captureFormat.BitsPerSample / 8;
                        captured[channel]
                            .Add(
                                flags.HasFlag(AudioClientBufferFlags.Silent)
                                    ? 0
                                    : ReadSample(bytes, offset, captureFormat)
                            );
                    }
                }
            }

            Thread.Sleep(2);
        }

        renderClient.Stop();
        captureClient.Stop();
        TimeEndPeriod(1);
        return captured;
    }

    /// <summary>
    /// The power of one frequency component (Goertzel), comparable to the mean square of the signal.
    /// </summary>
    private static double TonePower(double[] samples, int sampleRate, double frequency)
    {
        var coefficient = 2 * Math.Cos(2 * Math.PI * frequency / sampleRate);
        double s1 = 0,
            s2 = 0;
        foreach (var sample in samples)
        {
            var s0 = sample + coefficient * s1 - s2;
            s2 = s1;
            s1 = s0;
        }

        var magnitudeSquared = s1 * s1 + s2 * s2 - coefficient * s1 * s2;
        return 2 * magnitudeSquared / ((double)samples.Length * samples.Length);
    }

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static extern uint TimeBeginPeriod(uint milliseconds);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static extern uint TimeEndPeriod(uint milliseconds);

    private static string Try(
        MMDevice device,
        WaveFormat format,
        AudioClientShareMode mode,
        AudioClientStreamFlags flags
    )
    {
        try
        {
            using var client = Initialize(device, format, mode, flags);
            return "ok";
        }
        catch (Exception ex)
        {
            return ErrorName(ex);
        }
    }

    private static void WriteSample(byte[] data, int offset, WaveFormat format, double value)
    {
        var isFloat = IsFloat(format);
        switch (format.BitsPerSample)
        {
            case 16:
                BitConverter.TryWriteBytes(
                    data.AsSpan(offset),
                    (short)Math.Clamp(Math.Round(value * 32768), short.MinValue, short.MaxValue)
                );
                break;
            case 24:
                var sample24 = (int)Math.Clamp(Math.Round(value * 8388608), -8388608, 8388607);
                data[offset] = (byte)sample24;
                data[offset + 1] = (byte)(sample24 >> 8);
                data[offset + 2] = (byte)(sample24 >> 16);
                break;
            case 32 when isFloat:
                BitConverter.TryWriteBytes(data.AsSpan(offset), (float)value);
                break;
            case 32:
                BitConverter.TryWriteBytes(
                    data.AsSpan(offset),
                    (int)Math.Clamp(Math.Round(value * 2147483648.0), int.MinValue, int.MaxValue)
                );
                break;
        }
    }
}
