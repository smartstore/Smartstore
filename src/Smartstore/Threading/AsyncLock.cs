using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Smartstore.Threading
{
    public sealed class AsyncLock : IHideObjectMembers
    {
        #region static

        private static readonly TimeSpan _noTimeout = TimeSpan.FromMilliseconds(-1);
        private static readonly ConcurrentDictionary<object, AsyncLock> _keyedLocks = new ConcurrentDictionary<object, AsyncLock>();

        public static bool IsLockHeld(object key)
        {
            return _keyedLocks.ContainsKey(key);
        }

        public static IDisposable Keyed(object key, TimeSpan? timeout = null)
        {
            var keyedLock = GetOrCreateLock(key);
            return keyedLock.Lock(timeout);
        }

        public static Task<IDisposable> KeyedAsync(object key, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            var keyedLock = GetOrCreateLock(key);
            return keyedLock.LockAsync(timeout, cancellationToken);
        }

        private static AsyncLock GetOrCreateLock(object key)
        {
            Guard.NotNull(key, nameof(key));

            var item = _keyedLocks.GetOrAdd(key, k => new AsyncLock(key));
            item.IncrementCount();
            return item;
        }

        #endregion

        private int _waiterCount;

        private readonly object _key;
        private readonly SemaphoreSlim _semaphore;
        private readonly IDisposable _releaser;
        private readonly Task<IDisposable> _releaserTask;

        private AsyncLock(object key)
            : this()
        {
            _key = key;
        }

        public AsyncLock()
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _releaser = new Releaser(this);
            _releaserTask = Task.FromResult(_releaser);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IncrementCount() => Interlocked.Increment(ref _waiterCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecrementCount() => Interlocked.Decrement(ref _waiterCount);

        public IDisposable Lock(TimeSpan? timeout = null)
        {
            _semaphore.Wait(timeout ?? _noTimeout);

            return _releaser;
        }

        public Task<IDisposable> LockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            var wait = _semaphore.WaitAsync(timeout ?? _noTimeout, cancellationToken);

            return wait.IsCompleted
                ? _releaserTask
                : wait.ContinueWith(
                    (_, state) => ((AsyncLock)state)._releaser,
                    this,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        public readonly struct Releaser : IDisposable
        {
            private readonly AsyncLock _toRelease;

            public Releaser(AsyncLock toRelease)
            {
                _toRelease = toRelease;
            }

            public void Dispose()
            {
                if (_toRelease._key != null)
                {
                    _toRelease.DecrementCount();

                    if (_toRelease._waiterCount == 0)
                    {
                        // Remove from dict if keyed lock
                        _keyedLocks.TryRemove(_toRelease._key, out _);
                    }
                }

                if (_toRelease._semaphore.CurrentCount == 0)
                {
                    _toRelease._semaphore.Release();
                }

                _toRelease._semaphore.Dispose();
            }
        }
    }
}