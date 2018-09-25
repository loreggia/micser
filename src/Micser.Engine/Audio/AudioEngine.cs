using Micser.Engine.DataAccess;
using Micser.Shared.Models;
using System;
using System.Linq;

namespace Micser.Engine.Audio
{
    public class AudioEngine : IDisposable
    {
        public AudioEngine()
        {
        }

        ~AudioEngine()
        {
            ReleaseUnmanagedResources();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            Stop();

            using (var db = new Database())
            {
                var moduleDescriptions = db.GetCollection<AudioModuleDescription>().FindAll().ToArray();
                foreach (var description in moduleDescriptions)
                {
                    var module = Activator.CreateInstance(description.Type) as IAudioModule;
                }
            }
        }

        public void Stop()
        {
            throw new System.NotImplementedException();
        }

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }
    }
}