namespace Smartstore.Caching
{
    /// <summary>
    /// Contract for a concurrent string HashSet to be used in cache stores.
    /// </summary>
    public interface ISet : IEnumerable<string>
    {
        /// <summary>
        /// Attempts to add a value to the set.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>true if the value was added to the set. If the value already exists, this method returns false.</returns>
        bool Add(string value);

        /// <summary>
        /// Attempts to add many values to the set.
        /// </summary>
        void AddRange(IEnumerable<string> items);

        /// <summary>
        /// Clears the set.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determine whether the given value is in the set.
        /// </summary>
        /// <param name="item">The value to test.</param>
        /// <returns>true if the set contains the specified value; otherwise, false.</returns>
        bool Contains(string value);

        /// <summary>
        /// Attempts to remove a value from the set.
        /// </summary>
        /// <param name="item">The value to remove.</param>
        /// <returns>true if the value was removed successfully; otherwise false.</returns>
        bool Remove(string value);

        /// <summary>
        /// Move item from this set to the set at <paramref name="destinationKey"/>.
        /// When the specified <paramref name="value"/> already exists in the destination set, it is only removed from the source set.
        /// </summary>
        /// <param name="destinationKey">The key of the destination set.</param>
        /// <param name="value">The value to move.</param>
        /// <returns>
        /// <see langword="true"/> if the value is moved.
        /// <see langword="false"/> if the value is not a member of source and no operation was performed.
        /// </returns>
        bool Move(string destinationKey, string value);

        /// <summary>
        /// Combines the values from this set and the destination sets 
        /// at <paramref name="keys"/>, excluding duplicates.
        /// </summary>
        /// <param name="keys">Destination sets to combine with.</param>
        /// <returns>The number of values in this set after combining.</returns>
        long UnionWith(params string[] keys);

        /// <summary>
        /// Combines the values from this set and the destination sets 
        /// at <paramref name="keys"/>, including only intersecting values.
        /// </summary>
        /// <param name="keys">Destination sets to combine with.</param>
        /// <returns>The number of values in this set after combining.</returns>
        long IntersectWith(params string[] keys);

        /// <summary>
        /// Combines the values from this set and the destination sets 
        /// at <paramref name="keys"/>, including only the difference.
        /// </summary>
        /// <param name="keys">Destination sets to combine with.</param>
        /// <returns>The number of values in this set after combining.</returns>
        long ExceptWith(params string[] keys);

        /// <summary>
        /// Obtain the number of items in the set.
        /// </summary>
        /// <returns>The number of items in the set.</returns>
        int Count { get; }

        /// <inheritdoc cref="Add(string)"/>
        Task<bool> AddAsync(string value);

        /// <inheritdoc cref="AddRange(IEnumerable{string})"/>
        Task AddRangeAsync(IEnumerable<string> values);

        /// <inheritdoc cref="Clear()"/>
        Task ClearAsync();

        /// <inheritdoc cref="Contains(string)"/>
        Task<bool> ContainsAsync(string value);

        /// <inheritdoc cref="Remove(string)"/>
        Task<bool> RemoveAsync(string value);

        /// <inheritdoc cref="Move(string, string)"/>
        Task<bool> MoveAsync(string destinationKey, string value);

        /// <inheritdoc cref="UnionWith(string[])"/>
        Task<long> UnionWithAsync(params string[] keys);

        /// <inheritdoc cref="IntersectWith(string[])"/>
        Task<long> IntersectWithAsync(params string[] keys);

        /// <inheritdoc cref="ExceptWith(string[])"/>
        Task<long> ExceptWithAsync(params string[] keys);
    }
}
