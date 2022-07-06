#nullable enable

namespace Smartstore.Threading
{
    public interface IDistributedLock
    {
        string Key { get; }

        IDistributedLockHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default);

        IDistributedLockHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        ValueTask<IDistributedLockHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);

        ValueTask<IDistributedLockHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    }
}
