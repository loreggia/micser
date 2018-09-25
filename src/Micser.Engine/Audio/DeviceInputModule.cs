using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.Streams;
using Micser.Shared.Models;
using NLog;
using System;

namespace Micser.Engine.Audio
{
    public class DeviceInputModule : AudioModule
    {
        private WasapiCapture _capture;
        private DeviceDescription _deviceDescription;
        private ILogger _logger = LogManager.GetCurrentClassLogger();
        private SoundInSource _soundInSource;

        public DeviceDescription DeviceDescription
        {
            get => _deviceDescription;
            set
            {
                var oldId = _deviceDescription?.Id;
                _deviceDescription = value;

                if (oldId != _deviceDescription?.Id)
                {
                    InitializeDevice();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                DisposeCapture();
            }
        }

        private void DisposeCapture()
        {
            _soundInSource?.Dispose();
            _soundInSource = null;

            if (_capture != null)
            {
                _capture.Stop();
                _capture.Dispose();
                _capture = null;
            }
        }

        private void InitializeDevice()
        {
            DisposeCapture();

            if (DeviceDescription == null)
            {
                return;
            }

            using (var deviceEnumerator = new MMDeviceEnumerator())
            {
                var device = deviceEnumerator.GetDevice(DeviceDescription.Id);
                if (!device.DataFlow.HasFlag(DataFlow.Capture))
                {
                    return;
                }

                _capture = new WasapiCapture(true, AudioClientShareMode.Shared)
                {
                    Device = device
                };

                try
                {
                    _capture.Initialize();
                    _soundInSource = new SoundInSource(_capture) { FillWithZeros = true };
                    Output = _soundInSource;
                    _capture.Start();
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, ex);
                }
            }
        }
    }
}