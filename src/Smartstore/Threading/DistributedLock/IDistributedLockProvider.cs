namespace Smartstore.Threading
{
    /// <summary>
    /// Provider for <see cref="IDistributedLock"/> instances.
    /// </summary>
    public interface IDistributedLockProvider
    {
        /// <summary>
        /// Gets a <see cref="IDistributedLock"/> instance for the given <paramref name="key"/>.
        /// </summary>
        IDistributedLock GetLock(string key);
    }
}
