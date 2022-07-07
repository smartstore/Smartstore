namespace Smartstore.Threading
{
    /// <summary>
    /// A mutex synchronization primitive which can be used to coordinate access 
    /// to a resource or critical region of code across processes or systems. 
    /// The scope and capabilities of the lock are dependent on the particular implementation
    /// </summary>
    public interface IDistributedLock
    {
        /// <summary>
        /// A key that uniquely identifies the lock.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Checks - synchronously - whether a lock is already held.
        /// </summary>
        bool IsHeld();

        /// <summary>
        /// Checks - asynchronously - whether a lock is already held.
        /// </summary>
        Task<bool> IsHeldAsync();

        /// <summary>
        /// Acquires the lock synchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Usage: 
        /// <code>
        ///     using (myLock.Acquire(...))
        ///     {
        ///         /* we have the lock! */
        ///     }
        ///     // releases the lock
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt.</param>
        /// <param name="cancelToken">Specifies a token by which the wait can be canceled.</param>
        /// <returns>An <see cref="ILockHandle"/> which can be used to release the lock.</returns>
        ILockHandle Acquire(TimeSpan timeout, CancellationToken cancelToken = default);

        /// <summary>
        /// Acquires the lock asynchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Usage: 
        /// <code>
        ///     await using (await myLock.AcquireAsync(...))
        ///     {
        ///         /* we have the lock! */
        ///     }
        ///     // releases the lock
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt.</param>
        /// <param name="cancelToken">Specifies a token by which the wait can be canceled.</param>
        /// <returns>An <see cref="ILockHandle"/> which can be used to release the lock.</returns>
        Task<ILockHandle> AcquireAsync(TimeSpan timeout, CancellationToken cancelToken = default);
    }
}
