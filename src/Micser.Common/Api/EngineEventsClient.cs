using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Micser.Common.Api
{
    public class EngineEventsClient : EngineEventsRpcService.EngineEventsRpcServiceClient, IDisposable
    {
        private readonly object _streamLock = new object();
        private AsyncServerStreamingCall<EngineEvent> _stream;

        public EngineEventsClient(ChannelBase channel)
            : base(channel)
        {
        }

        public void Connect()
        {
            if (_stream == null)
            {
                lock (_streamLock)
                {
                    _stream ??= Connect(new Empty());
                }
            }

            Task.Run(async () =>
            {
                try
                {
                    while (await _stream.ResponseStream.MoveNext())
                    {
                        var engineEvent = _stream.ResponseStream.Current;
                        // TODO OnEvent
                    }
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    _stream = null;
                }
            }).Start();
        }

        public void Disconnect()
        {
            _stream?.Dispose();
        }

        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }
    }
}