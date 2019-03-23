using System;

namespace Micser.Common.Api
{
    public interface IApiServer : IDisposable
    {
        void Start();

        void Stop();
    }
}