using System;
using System.Threading.Tasks;
using Grpc.Core;
using Prism.Events;

namespace Micser.Plugins.Main.Api
{
    public class SpectrumApiClient : SpectrumRpcService.SpectrumRpcServiceClient, IDisposable
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly object _streamLock = new object();
        private AsyncServerStreamingCall<SpectrumData> _stream;

        public SpectrumApiClient(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
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
                        var data = _stream.ResponseStream.Current;
                        _eventAggregator.GetEvent<SpectrumDataEvent>().Publish(data);
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