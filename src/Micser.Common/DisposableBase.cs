using System;

namespace Micser.Common
{
    /// <summary>
    /// Base class implementing the disposable pattern.
    /// </summary>
    public abstract class DisposableBase : IDisposable
    {
        /// <summary>
        /// Destructor (calls Dispose(false)).
        /// </summary>
        ~DisposableBase()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        /// <param name="disposing">If true, the call originates from <see cref="IDisposable.Dispose"/>, otherwise from the destructor.</param>
        protected abstract void Dispose(bool disposing);
    }
}