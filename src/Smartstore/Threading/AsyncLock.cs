using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Smartstore.Threading
{
    public sealed class AsyncLock : IHideObjectMembers
    {
        #region static

        static readonly ConcurrentDictionary<object, AsyncLock> _keyedLocks = new();

        public static bool IsLockHeld(object key)
        {
            return _keyedLocks.ContainsKey(key);
        }

        public static ILockHandle Keyed(object key, TimeSpan? timeout = null, CancellationToken cancelToken = default)
        {
            var keyedLock = GetOrCreateLock(key);
            return keyedLock.Lock(timeout, cancelToken);
        }

        public static Task<ILockHandle> KeyedAsync(object key, TimeSpan? timeout = null, CancellationToken cancelToken = default)
        {
            var keyedLock = GetOrCreateLock(key);
            return keyedLock.LockAsync(timeout, cancelToken);
        }

        internal static AsyncLock GetOrCreateLock(object key)
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

        private AsyncLock(object key)
            : this()
        {
            _key = key;
        }

        public AsyncLock()
        {
            _semaphore = new SemaphoreSlim(1, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IncrementCount() => Interlocked.Increment(ref _waiterCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecrementCount() => Interlocked.Decrement(ref _waiterCount);

        public ILockHandle Lock(TimeSpan? timeout = null, CancellationToken cancelToken = default)
        {
            _semaphore.Wait(timeout ?? Timeout.InfiniteTimeSpan, cancelToken);
            return new AsyncLockHandle(this);
        }

        public Task<ILockHandle> LockAsync(TimeSpan? timeout = null, CancellationToken cancelToken = default)
        {
            var handle = new AsyncLockHandle(this) as ILockHandle;
            var wait = _semaphore.WaitAsync(timeout ?? Timeout.InfiniteTimeSpan, cancelToken);

            return wait.IsCompleted
                ? Task.FromResult(handle)
                : wait.ContinueWith(
                    (_, state) => (ILockHandle)state,
                    handle,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        public readonly struct AsyncLockHandle : ILockHandle
        {
            private readonly AsyncLock _lock;

            public AsyncLockHandle(AsyncLock @lock)
            {
                _lock = @lock;
            }

            public ValueTask DisposeAsync()
                => new(ReleaseAsync());

            public void Dispose()
                => Release();

            public Task ReleaseAsync()
            {
                Release();
                return Task.CompletedTask;
            }

            public void Release()
            {
                if (_lock._key != null)
                {
                    if (_lock._waiterCount > 0)
                    {
                        _lock.DecrementCount();
                    }

                    if (_lock._waiterCount == 0)
                    {
                        // Remove from dict if keyed lock
                        _keyedLocks.TryRemove(_lock._key, out _);
                    }
                }

                if (_lock._semaphore.CurrentCount == 0)
                {
                    _lock._semaphore.Release();
                }
            }
        }
    }
}