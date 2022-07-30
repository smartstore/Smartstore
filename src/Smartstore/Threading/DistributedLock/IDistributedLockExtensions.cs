namespace Smartstore.Threading
{
    public static class IDistributedLockExtensions
    {
        static readonly TimeSpan NoTimeout = TimeSpan.FromMilliseconds(0);

        /// <inheritdoc cref="IDistributedLock.Acquire(TimeSpan, CancellationToken)"/>
        /// <summary>
        /// Acquires the lock synchronously with infinite timeout. Usage: 
        /// <code>
        ///     using (myLock.Acquire())
        ///     {
        ///         /* we have the lock! */
        ///     }
        ///     // releases the lock
        /// </code>
        /// </summary>
        public static ILockHandle Acquire(this IDistributedLock @lock, CancellationToken cancelToken = default)
        {
            return @lock.Acquire(Timeout.InfiniteTimeSpan, cancelToken);
        }

        /// <inheritdoc cref="TryAcquire(IDistributedLock, TimeSpan, out ILockHandle)"/>
        /// <summary>
        /// Attempts to acquire the lock synchronously but without blocking.
        /// That is, if the current thread can acquire the lock without blocking, 
        /// it will do so and TRUE will be returned, otherwise FALSE will be returned.
        /// Usage: 
        /// <code>
        ///     if (myLock.TryAcquire(out var lockHandle))
        ///     {
        ///         // Do something and release...
        ///         lockHandle.Release();
        ///     }
        /// </code>
        /// </summary>
        public static bool TryAcquire(this IDistributedLock @lock, out ILockHandle lockHandle)
        {
            return TryAcquire(@lock, NoTimeout, out lockHandle);
        }

        /// <summary>
        /// Attempts to acquire the lock synchronously. Usage: 
        /// <code>
        ///     if (myLock.TryAcquire(..., out var lockHandle))
        ///     {
        ///         // Do something and release...
        ///         lockHandle.Release();
        ///     }
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt.</param>
        /// <param name="lockHandle">An <see cref="ILockHandle"/> which can be used to release the lock.</param>
        /// <returns><c>true</c> if the lock was acquired, <c>false</c> otherwise.</returns>
        public static bool TryAcquire(this IDistributedLock @lock, TimeSpan timeout, out ILockHandle lockHandle)
        {
            lockHandle = null;

            try
            {
                lockHandle = @lock.Acquire(timeout);
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <inheritdoc cref="IDistributedLock.AcquireAsync(TimeSpan, CancellationToken)"/>
        /// <summary>
        /// Acquires the lock asynchronously with infinite timeout. Usage: 
        /// <code>
        ///     await using (await myLock.AcquireAsync())
        ///     {
        ///         /* we have the lock! */
        ///     }
        ///     // releases the lock
        /// </code>
        /// </summary>
        public static Task<ILockHandle> AcquireAsync(this IDistributedLock @lock, CancellationToken cancelToken = default)
        {
            return @lock.AcquireAsync(Timeout.InfiniteTimeSpan, cancelToken);
        }

        /// <inheritdoc cref="TryAcquireAsync(IDistributedLock, TimeSpan, CancellationToken)"/>
        /// <summary>
        /// Attempts to acquire the lock asynchronously but without blocking.
        /// That is, if the current thread can acquire the lock without blocking, 
        /// it will do so and TRUE will be returned, otherwise FALSE will be returned.
        /// Usage: 
        /// <code>
        ///     if ((await myLock.TryAcquireAsync()).Out(out var lockHandle))
        ///     {
        ///         // Do something and release...
        ///         await lockHandle.ReleaseAsync();
        ///     }
        /// </code>
        /// </summary>
        public static Task<AsyncOut<ILockHandle>> TryAcquireAsync(this IDistributedLock @lock, CancellationToken cancelToken = default)
        {
            return TryAcquireAsync(@lock, NoTimeout, cancelToken);
        }

        /// <summary>
        /// Attempts to acquire the lock asynchronously. Usage: 
        /// <code>
        ///     if ((await myLock.TryAcquireAsync(...)).Out(out var lockHandle))
        ///     {
        ///         // Do something and release...
        ///         await lockHandle.ReleaseAsync();
        ///     }
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt.</param>
        /// <param name="cancelToken">Specifies a token by which the wait can be canceled.</param>
        public static async Task<AsyncOut<ILockHandle>> TryAcquireAsync(this IDistributedLock @lock, TimeSpan timeout, CancellationToken cancelToken = default)
        {
            try
            {
                var lockHandle = await @lock.AcquireAsync(timeout, cancelToken);
                return new AsyncOut<ILockHandle>(true, lockHandle);
            }
            catch
            {
                return AsyncOut<ILockHandle>.Empty;
            }
        }
    }
}
