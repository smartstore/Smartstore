using System.Collections;
using System.Diagnostics;

namespace Smartstore
{
    /// <summary>
    /// Base class for disposable objects.
    /// </summary>
    public abstract class Disposable : IDisposable, IAsyncDisposable
    {
        private const int DisposedFlag = 1;
        private int _isDisposed;

        [DebuggerStepThrough]
        public void Dispose()
        {
            var wasDisposed = Interlocked.Exchange(ref _isDisposed, DisposedFlag);
            if (wasDisposed == DisposedFlag)
            {
                return;
            }

            OnDispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void OnDispose(bool disposing)
        {
        }

        [DebuggerStepThrough]
        public ValueTask DisposeAsync()
        {
            // Still need to check if we've already disposed; can't do both.
            var wasDisposed = Interlocked.Exchange(ref _isDisposed, DisposedFlag);
            if (wasDisposed != DisposedFlag)
            {
                GC.SuppressFinalize(this);

                // Always true, but means we get the similar syntax as Dispose,
                // and separates the two overloads.
                return OnDisposeAsync(true);
            }

            return default;
        }

        /// <summary>
        ///  Releases unmanaged and - optionally - managed resources, asynchronously.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual ValueTask OnDisposeAsync(bool disposing)
        {
            // Default implementation does a synchronous dispose.
            OnDispose(disposing);
            return default;
        }

        /// <summary>
        /// Gets a value indicating whether the current instance has been disposed.
        /// </summary>
        protected internal bool IsDisposed
        {
            get
            {
                Interlocked.MemoryBarrier();
                return _isDisposed == DisposedFlag;
            }
        }

        #region Utils for inheritors

        [DebuggerStepThrough]
        protected internal void CheckDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        [DebuggerStepThrough]
        protected internal void CheckDisposed(string errorMessage)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName, errorMessage);
        }

        protected static void DisposeEnumerable(IEnumerable enumerable)
        {
            if (enumerable != null)
            {
                foreach (object obj2 in enumerable)
                {
                    DisposeMember(obj2);
                }
                DisposeMember(enumerable);
            }
        }

        protected static void DisposeDictionary<K, V>(IDictionary<K, V> dictionary)
        {
            if (dictionary != null)
            {
                foreach (KeyValuePair<K, V> pair in dictionary)
                {
                    DisposeMember(pair.Value);
                }
                DisposeMember(dictionary);
            }
        }

        protected static void DisposeDictionary(IDictionary dictionary)
        {
            if (dictionary != null)
            {
                foreach (IDictionaryEnumerator pair in dictionary)
                {
                    DisposeMember(pair.Value);
                }
                DisposeMember(dictionary);
            }
        }

        protected static void DisposeMember(object member)
        {
            if (member is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        #endregion
    }
}