namespace Smartstore.Threading
{
    /// <summary>
    /// A handle to a lock or other synchronization primitive. To unlock/release,
    /// simply dispose the handle or call <see cref="Release()"/> / <see cref="ReleaseAsync()"/>
    /// </summary>
    public interface ILockHandle : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Releases the lock synchronously.
        /// </summary>
        void Release();

        /// <summary>
        /// Releases the lock asynchronously.
        /// </summary>
        Task ReleaseAsync();
    }

    public sealed class NullLockHandle : ILockHandle
    {
        public static readonly NullLockHandle Instance = new();

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;

        public void Release()
        {
        }

        public Task ReleaseAsync()
            => Task.CompletedTask;
    }
}
