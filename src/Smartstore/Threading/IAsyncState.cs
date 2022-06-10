using Smartstore.Caching;

namespace Smartstore.Threading
{
    [Serializable]
    public class AsyncStateInfo
    {
        // used for serialization compatibility
        public static readonly string Version = "1";

        public object Progress { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime LastAccessUtc { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Stores status information about long-running processes.
    /// </summary>
    public interface IAsyncState
    {
        /// <summary>
        /// Gets the underlying storage provider.
        /// </summary>
        ICacheStore Store { get; }

        /// <summary>
        /// Checks whether a status object exists.
        /// </summary>
        /// <typeparam name="T">The type of status to check for.</typeparam>
        /// <param name="name">The optional identifier.</param>
        bool Contains<T>(string name = null);

        /// <summary>
        /// Gets the status object.
        /// </summary>
        /// <typeparam name="T">The type of status to retrieve.</typeparam>
        /// <param name="name">The optional identifier.</param>
        /// <returns>The status object instance or <c>null</c> if it doesn't exist.</returns>
		T Get<T>(string name = null);

        /// <summary>
        /// Gets the status object.
        /// </summary>
        /// <typeparam name="T">The type of status to retrieve.</typeparam>
        /// <param name="name">The optional identifier.</param>
        /// <returns>The status object instance or <c>null</c> if it doesn't exist.</returns>
        Task<T> GetAsync<T>(string name = null);

        /// <summary>
        /// Enumerates all currently available status objects of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Status type to enumerate.</typeparam>
		IEnumerable<T> GetAll<T>();

        /// <summary>
        /// Creates a status entry for a long-running process. The key is <typeparamref name="T"/> + <paramref name="name"/>.
        /// When "Redis" is active the item will be saved in the Redis store so that all nodes in a web farm have access to the object.
        /// If an object with the same key already exists it will be removed first.
        /// </summary>
        /// <typeparam name="T">The type of status object.</typeparam>
        /// <param name="state">The initial status object instance. Can be anything but should be serializable.</param>
        /// <param name="name">The optional identifier. Without identifier, any item of type <typeparamref name="T"/> will be overwritten.</param>
        /// <param name="neverExpires">The default sliding expiration time is 15 minutes. Pass <c>true</c> to prevent automatic expiration but be sure to remove the item.</param>
        /// <param name="cancelTokenSource">The cancellation token source to associate the status with. Cancelling this source instance will hopefully cancel the process.</param>
        void Create<T>(T state, string name = null, bool neverExpires = false, CancellationTokenSource cancelTokenSource = default);

        /// <summary>
        /// Creates a status entry for a long-running process. The key is <typeparamref name="T"/> + <paramref name="name"/>.
        /// When "Redis" is active the item will be saved in the Redis store so that all nodes in a web farm have access to the object.
        /// If an object with the same key already exists it will be removed first.
        /// </summary>
        /// <typeparam name="T">The type of status object.</typeparam>
        /// <param name="state">The initial status object instance. Can be anything but should be serializable.</param>
        /// <param name="name">The optional identifier. Without identifier, any item of type <typeparamref name="T"/> will be overwritten.</param>
        /// <param name="neverExpires">The default sliding expiration time is 15 minutes. Pass <c>true</c> to prevent automatic expiration but be sure to remove the item.</param>
        /// <param name="cancelTokenSource">The cancellation token source to associate the status with. Cancelling this source instance will hopefully cancel the process.</param>
        Task CreateAsync<T>(T state, string name = null, bool neverExpires = false, CancellationTokenSource cancelTokenSource = default);

        /// <summary>
        /// Updates an existing status object. Call this if your unit of work made any progress.
        /// Ensure that the status object has been created by calling <see cref="Create{T}(T, string, bool)"/> 
        /// </summary>
        /// <typeparam name="T">The type of status object.</typeparam>
        /// <param name="update">The update action delegate</param>
        /// <param name="name">The optional identifier.</param>
        /// <returns><c>false</c> if the status object does not exist, <c>true</c> otherwise.</returns>
		bool Update<T>(Action<T> update, string name = null);

        /// <summary>
        /// Updates an existing status object. Call this if your unit of work made any progress.
        /// Ensure that the status object has been created by calling <see cref="Create{T}(T, string, bool)"/> 
        /// </summary>
        /// <typeparam name="T">The type of status object.</typeparam>
        /// <param name="update">The update action delegate</param>
        /// <param name="name">The optional identifier.</param>
        /// <returns><c>false</c> if the status object does not exist, <c>true</c> otherwise.</returns>
        Task<bool> UpdateAsync<T>(Action<T> update, string name = null);

        /// <summary>
        /// Removes a status object. The cancellation token associated with this key will be disposed.
        /// </summary>
        /// <typeparam name="T">The type of status object to remove.</typeparam>
        /// <param name="name">The optional identifier of the object to remove.</param>
		void Remove<T>(string name = null);

        /// <summary>
        /// Removes a status object. The cancellation token associated with this key will be disposed.
        /// </summary>
        /// <typeparam name="T">The type of status object to remove.</typeparam>
        /// <param name="name">The optional identifier of the object to remove.</param>
        Task RemoveAsync<T>(string name = null);

        /// <summary>
        /// Requests a cancellation for the given process.
        /// </summary>
        /// <typeparam name="T">The type of status object to remove.</typeparam>
        /// <param name="name">The optional identifier of the object to remove.</param>
        /// <returns><c>true</c> if the cancellation could be requested, <c>false</c> otherwise.</returns>
        bool Cancel<T>(string name = null);
    }
}