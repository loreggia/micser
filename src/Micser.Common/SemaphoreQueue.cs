using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.Common
{
    /// <summary>
    /// An async semaphore that also retains the order in which the lock requests were made.
    /// </summary>
    public class SemaphoreQueue : IDisposable
    {
        private static readonly Task Completed = Task.FromResult(true);
        private readonly Queue<TaskCompletionSource<bool>> _waiters = new Queue<TaskCompletionSource<bool>>();
        private int _currentCount;

        /// <summary>
        /// Creates an instance of the <see cref="SemaphoreQueue"/> class using a maximum initial count.
        /// </summary>
        /// <param name="initialCount">The initial number of requests for the semaphore that can be granted concurrently.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCount"/> must be greater than or equal to 0.</exception>
        public SemaphoreQueue(int initialCount)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCount));
            }

            _currentCount = initialCount;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases a request (increases the number of available requests by 1).
        /// </summary>
        public void Release()
        {
            TaskCompletionSource<bool> toRelease = null;

            lock (_waiters)
            {
                if (_waiters.Count > 0)
                {
                    toRelease = _waiters.Dequeue();
                }
                else
                {
                    ++_currentCount;
                }
            }

            toRelease?.SetResult(true);
        }

        /// <summary>
        /// Waits until there is an available request slot (decreases the number of available requests by 1).
        /// </summary>
        /// <returns></returns>
        public Task WaitAsync()
        {
            lock (_waiters)
            {
                if (_currentCount > 0)
                {
                    --_currentCount;
                    return Completed;
                }

                var waiter = new TaskCompletionSource<bool>();
                _waiters.Enqueue(waiter);
                return waiter.Task;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                while (_waiters.Count > 0)
                {
                    Release();
                }
            }
        }
    }
}