namespace Smartstore.Threading
{
    internal class DistributedSemaphoreLockProvider : IDistributedLockProvider
    {
        public static DistributedSemaphoreLockProvider Instance { get; } = new DistributedSemaphoreLockProvider();

        public IDistributedLock GetLock(string key)
        {
            Guard.NotEmpty(key);
            return new DistributedSemaphoreLock(key);
        }
    }

    public sealed class DistributedSemaphoreLock : IDistributedLock
    {
        public DistributedSemaphoreLock(string key)
        {
            Key = key;
        }

        public string Key { get; }

        public bool IsHeld()
        {
            return AsyncLock.IsLockHeld(Key);
        }

        public Task<bool> IsHeldAsync()
        {
            return Task.FromResult(AsyncLock.IsLockHeld(Key));
        }

        public ILockHandle Acquire(TimeSpan timeout, CancellationToken cancelToken = default)
        {
            return AsyncLock.Keyed(Key, timeout, cancelToken);
        }

        public Task<ILockHandle> AcquireAsync(TimeSpan timeout, CancellationToken cancelToken = default)
        {
            return AsyncLock.KeyedAsync(Key, timeout, cancelToken);
        }
    }
}
