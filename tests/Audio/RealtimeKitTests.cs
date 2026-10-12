using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Versioning;
using Tmds.DBus.Protocol;

namespace Micser.Audio.Tests;

/// <summary>
/// <see cref="RealtimeKit"/> against a fake RTKit on a private D-Bus (<c>dbus-daemon</c>), which only records the calls, so no thread's
/// priority changes; skipped without <c>dbus-daemon</c>.
/// </summary>
[SupportedOSPlatform("linux")]
public class RealtimeKitTests
{
    private const long ThreadId = 1234;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Test]
    public async Task Acquire_GetsARealtimePriorityBelowRealtimeKitsMaximum()
    {
        await using var bus = await PrivateBus.StartAsync();
        await using var rtkit = await FakeRealtimeKit.StartAsync(bus.Address, refusesRealtime: false);

        var result = await AcquireAsync(bus.Address, 20);

        await Assert.That(result).IsEqualTo(new RealtimeKitResult(19, null, null));
        await Assert.That(rtkit.Calls).IsEquivalentTo(["MakeThreadRealtime 1234 19"]);
        LinuxNativeMethods.GetRLimit(LinuxNativeMethods.RlimitRtTime, out var limit);
        await Assert.That(limit.Maximum).IsLessThanOrEqualTo(FakeRealtimeKit.MaxRealtimeTime);
    }

    [Test]
    public async Task Acquire_LowerPriority_KeepsIt()
    {
        await using var bus = await PrivateBus.StartAsync();
        await using var rtkit = await FakeRealtimeKit.StartAsync(bus.Address, refusesRealtime: false);

        var result = await AcquireAsync(bus.Address, 5);

        await Assert.That(result.Priority).IsEqualTo(5);
    }

    [Test]
    public async Task Acquire_RealtimeRefused_RaisesTheNiceLevel()
    {
        await using var bus = await PrivateBus.StartAsync();
        await using var rtkit = await FakeRealtimeKit.StartAsync(bus.Address, refusesRealtime: true);

        var result = await AcquireAsync(bus.Address, 20);

        await Assert.That(result.Priority).IsNull();
        await Assert.That(result.Nice).IsEqualTo(-11);
        await Assert.That(result.Error).Contains("Not allowed");
        await Assert
            .That(rtkit.Calls)
            .IsEquivalentTo(["MakeThreadRealtime 1234 19", "MakeThreadHighPriority 1234 -11"]);
    }

    [Test]
    public async Task Acquire_WithoutRealtimeKit_ReportsTheError()
    {
        await using var bus = await PrivateBus.StartAsync();

        var result = await AcquireAsync(bus.Address, 20);

        await Assert.That(result.Priority).IsNull();
        await Assert.That(result.Nice).IsNull();
        await Assert.That(result.Error).IsNotNull();
    }

    /// <summary>
    /// <see cref="RealtimeKit.Acquire"/> on a thread of its own: after awaiting a D-Bus reply the test continues on the fake's receive
    /// loop, which the blocking call would stop.
    /// </summary>
    private static Task<RealtimeKitResult> AcquireAsync(string address, int priority)
    {
        return Task.Run(() => RealtimeKit.Acquire(address, ThreadId, priority, Timeout));
    }

    /// <summary>
    /// A private session bus; skips the test without <c>dbus-daemon</c>.
    /// </summary>
    private sealed class PrivateBus : IAsyncDisposable
    {
        private readonly Process _process;

        private PrivateBus(Process process, string address)
        {
            _process = process;
            Address = address;
        }

        public string Address { get; }

        public static async Task<PrivateBus> StartAsync()
        {
            Process? process = null;
            try
            {
                process = Process.Start(
                    new ProcessStartInfo("dbus-daemon", "--session --nofork --print-address=1")
                    {
                        RedirectStandardOutput = true,
                    }
                );
            }
            catch (System.ComponentModel.Win32Exception) { }

            Skip.When(process == null, "No dbus-daemon.");
            var address = await process!.StandardOutput.ReadLineAsync().WaitAsync(Timeout);
            return new PrivateBus(process, address!);
        }

        public async ValueTask DisposeAsync()
        {
            _process.Kill();
            await _process.WaitForExitAsync();
            _process.Dispose();
        }
    }

    /// <summary>
    /// RTKit's interface with Ubuntu's defaults (priority up to 20, nice down to -15, 200 ms realtime time), recording the calls.
    /// </summary>
    private sealed class FakeRealtimeKit : IPathMethodHandler, IAsyncDisposable
    {
        public const ulong MaxRealtimeTime = 200_000;

        private readonly ConcurrentQueue<string> _calls = new();
        private readonly DBusConnection _connection;
        private readonly bool _refusesRealtime;

        private FakeRealtimeKit(DBusConnection connection, bool refusesRealtime)
        {
            _connection = connection;
            _refusesRealtime = refusesRealtime;
        }

        public string[] Calls => [.. _calls];

        public bool HandlesChildPaths => false;

        public string Path => RealtimeKit.ObjectPath;

        public static async Task<FakeRealtimeKit> StartAsync(string address, bool refusesRealtime)
        {
            var connection = new DBusConnection(address);
            await connection.ConnectAsync();
            var rtkit = new FakeRealtimeKit(connection, refusesRealtime);
            connection.AddMethodHandler(rtkit);
            await connection.RequestNameAsync(RealtimeKit.ServiceName, RequestNameOptions.Default);
            return rtkit;
        }

        public ValueTask DisposeAsync()
        {
            _connection.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            var reader = context.Request.GetBodyReader();
            switch (context.Request.InterfaceAsString, context.Request.MemberAsString)
            {
                case ("org.freedesktop.DBus.Properties", "Get"):
                    reader.ReadString();
                    ReplyProperty(context, reader.ReadString());
                    break;
                case (RealtimeKit.ServiceName, "MakeThreadRealtime"):
                    _calls.Enqueue($"MakeThreadRealtime {reader.ReadUInt64()} {reader.ReadUInt32()}");
                    if (_refusesRealtime)
                    {
                        context.ReplyError("org.freedesktop.DBus.Error.AccessDenied", "Not allowed");
                    }
                    else
                    {
                        ReplyEmpty(context);
                    }

                    break;
                case (RealtimeKit.ServiceName, "MakeThreadHighPriority"):
                    _calls.Enqueue($"MakeThreadHighPriority {reader.ReadUInt64()} {reader.ReadInt32()}");
                    ReplyEmpty(context);
                    break;
                default:
                    context.ReplyUnknownMethodError();
                    break;
            }

            return ValueTask.CompletedTask;
        }

        private static void ReplyEmpty(MethodContext context)
        {
            using var writer = context.CreateReplyWriter(null!);
            context.Reply(writer.CreateMessage());
        }

        private static void ReplyProperty(MethodContext context, string name)
        {
            using var writer = context.CreateReplyWriter("v");
            switch (name)
            {
                case "MaxRealtimePriority":
                    writer.WriteVariantInt32(20);
                    break;
                case "MinNiceLevel":
                    writer.WriteVariantInt32(-15);
                    break;
                default:
                    writer.WriteVariantInt64((long)MaxRealtimeTime);
                    break;
            }

            context.Reply(writer.CreateMessage());
        }
    }
}
