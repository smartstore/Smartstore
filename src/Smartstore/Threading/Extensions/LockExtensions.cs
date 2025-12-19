using System.Diagnostics;
using Smartstore.Utilities;

namespace Smartstore.Threading;

public static class LockExtensions
{
    extension (ReaderWriterLockSlim rwLock)
    {
        /// <summary>
        /// Acquires a disposable reader lock that can be used with a using statement.
        /// </summary>
        /// <param name="timeout">The time to wait, or null to wait indefinitely.</param>
        [DebuggerStepThrough]
        public IDisposable GetReadLock(TimeSpan? timeout = null)
        {
            bool acquire = rwLock.IsReadLockHeld == false ||
                           rwLock.RecursionPolicy == LockRecursionPolicy.SupportsRecursion;

            if (acquire)
            {
                if (rwLock.TryEnterReadLock(timeout ?? Timeout.InfiniteTimeSpan))
                {
                    return new ActionDisposable(rwLock.ExitReadLock);
                }
            }

            return ActionDisposable.Empty;
        }

        /// <summary>
        /// Acquires a disposable and upgradeable reader lock that can be used with a using statement.
        /// </summary>
        /// <param name="timeout">The time to wait, or null to wait indefinitely.</param>
        [DebuggerStepThrough]
        public IDisposable GetUpgradeableReadLock(TimeSpan? timeout = null)
        {
            bool acquire = rwLock.IsUpgradeableReadLockHeld == false ||
                           rwLock.RecursionPolicy == LockRecursionPolicy.SupportsRecursion;

            if (acquire)
            {
                if (rwLock.TryEnterUpgradeableReadLock(timeout ?? Timeout.InfiniteTimeSpan))
                {
                    return new ActionDisposable(rwLock.ExitUpgradeableReadLock);
                }
            }

            return ActionDisposable.Empty;
        }

        /// <summary>
        /// Tries to enter a disposable write lock that can be used with a using statement.
        /// </summary>
        /// <param name="timeout">The time to wait, or null to wait indefinitely.</param>
        [DebuggerStepThrough]
        public IDisposable GetWriteLock(TimeSpan? timeout = null)
        {
            bool acquire = rwLock.IsWriteLockHeld == false ||
                           rwLock.RecursionPolicy == LockRecursionPolicy.SupportsRecursion;

            if (acquire)
            {
                if (rwLock.TryEnterWriteLock(timeout ?? Timeout.InfiniteTimeSpan))
                {
                    return new ActionDisposable(rwLock.ExitWriteLock);
                }
            }

            return ActionDisposable.Empty;
        }
    }
}
