﻿using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using Micser.Common;
using Micser.Common.Devices;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure;
using System;
using System.Diagnostics;
using System.Linq;

namespace Micser.Plugins.Main.Modules
{
    public class DeviceOutputModule : AudioModule
    {
        private const string DeviceIdKey = "DeviceId";
        private DeviceDescription _deviceDescription;
        private int _latency;
        private WasapiOut _output;

        public DeviceOutputModule()
        {
            Latency = 25;
        }

        /// <summary>
        ///     Gets or sets the output device description. The <see cref="DeviceDescription.Id" />-Property is used to select the device.
        /// </summary>
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

        /// <summary>
        ///     Gets or sets the output latency in milliseconds.
        /// </summary>
        public int Latency
        {
            get => _latency;
            set
            {
                if (_latency != value)
                {
                    _latency = value;
                    InitializeDevice();
                }
            }
        }

        public override StateDictionary GetState()
        {
            return new StateDictionary
            {
                { DeviceIdKey, DeviceDescription?.Id }
            };
        }

        public override void Initialize(ModuleDescription description)
        {
            base.Initialize(description);

            var deviceId = description.State?.GetObject<string>(DeviceIdKey);
            if (deviceId != null)
            {
                var deviceService = new DeviceService();
                DeviceDescription = deviceService.GetDevices(DeviceType.Output).FirstOrDefault(d => d.Id == deviceId);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                DisposeOutput();
            }
        }

        protected override void OnInputChanged()
        {
            base.OnInputChanged();

            InitializeDevice();
        }

        protected override void OnInputOutputChanged(object sender, EventArgs e)
        {
            base.OnInputOutputChanged(sender, e);

            InitializeDevice();
        }

        private void DisposeOutput()
        {
            if (_output != null)
            {
                _output.Stopped -= OnOutputStopped;
                _output.Stop();
                _output.Dispose();
                _output = null;
            }
        }

        private void InitializeDevice()
        {
            DisposeOutput();

            if (string.IsNullOrEmpty(DeviceDescription?.Id) || Input?.Output == null)
            {
                return;
            }

            using (var deviceEnumerator = new MMDeviceEnumerator())
            {
                var device = deviceEnumerator.GetDevice(DeviceDescription.Id);
                if (!device.DataFlow.HasFlag(DataFlow.Render))
                {
                    return;
                }

                _output = new WasapiOut(true, AudioClientShareMode.Shared, Latency)
                {
                    Device = device
                };
                _output.Initialize(Input.Output);
                _output.Stopped += OnOutputStopped;
                _output.Play();
            }
        }

        private void OnOutputStopped(object sender, PlaybackStoppedEventArgs e)
        {
            Debug.WriteLine("Warning: Output stopped!");
            //_output.Play();
        }
    }
}