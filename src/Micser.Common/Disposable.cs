using System;

namespace Micser.Common
{
    public class Disposable : IDisposable
    {
        private readonly Action _onDispose;

        public Disposable(Action onDispose)
        {
            _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
        }

        public void Dispose()
        {
            _onDispose();
            GC.SuppressFinalize(this);
        }
    }
}