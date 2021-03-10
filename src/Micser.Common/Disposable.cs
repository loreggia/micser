using System;

namespace Micser.Common
{
    /// <summary>
    /// Disposable wrapper for an action.
    /// </summary>
    public class Disposable : IDisposable
    {
        private readonly Action _onDispose;

        /// <summary>
        /// Creates a new instance of the <see cref="Disposable"/> class.
        /// </summary>
        /// <param name="onDispose">The action to execute when this instance is disposed.</param>
        public Disposable(Action onDispose)
        {
            _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _onDispose();
            GC.SuppressFinalize(this);
        }
    }
}