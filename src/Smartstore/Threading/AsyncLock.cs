using System.Runtime.CompilerServices;
using AsyncKeyedLock;

namespace Smartstore.Threading
{
    public sealed class AsyncLock : IHideObjectMembers
    {
        #region static

        static readonly AsyncKeyedLocker<string> _asyncKeyedLock = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLockHeld(string key)
        {
            return _asyncKeyedLock.IsInUse(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ILockHandle Keyed(string key, TimeSpan? timeout = null, CancellationToken cancelToken = default)
        {
            return new AsyncLockHandle(_asyncKeyedLock.LockOrNull(key, timeout ?? Timeout.InfiniteTimeSpan, cancelToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<ILockHandle> KeyedAsync(string key, TimeSpan? timeout = null, CancellationToken cancelToken = default)
        {
            return new AsyncLockHandle(await _asyncKeyedLock.LockOrNullAsync(key, timeout ?? Timeout.InfiniteTimeSpan, cancelToken).ConfigureAwait(false));
        }

        #endregion

        private readonly SemaphoreSlim _semaphore;

        public AsyncLock()
        {
            _semaphore = new SemaphoreSlim(1, 1);
        }

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
            private readonly IDisposable _asyncKeyedLockReleaser;

            public AsyncLockHandle(AsyncLock @lock)
            {
                _lock = @lock;
            }

            public AsyncLockHandle(IDisposable asyncKeyedLockReleaser)
            {
                _asyncKeyedLockReleaser = asyncKeyedLockReleaser;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ValueTask DisposeAsync()
                => new(ReleaseAsync());

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
                => Release();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Task ReleaseAsync()
            {
                Release();
                return Task.CompletedTask;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Release()
            {
                if (_lock == default)
                {
                    _asyncKeyedLockReleaser?.Dispose();
                }
                else
                {
                    if (_lock._semaphore.CurrentCount == 0)
                    {
                        _lock._semaphore.Release();
                    }
                }
            }
        }
    }
}