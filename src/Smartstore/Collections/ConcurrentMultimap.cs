#nullable enable

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Smartstore.Collections.JsonConverters;
using System.Text.Json.Serialization;

namespace Smartstore.Collections;

/// <summary>
/// A thread-safe data structure that contains multiple values for each key.
/// </summary>
/// <typeparam name="TKey">The type of key.</typeparam>
/// <typeparam name="TValue">The type of value.</typeparam>
[JsonConverter(typeof(MultimapJsonConverterFactory))]
public class ConcurrentMultimap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, SyncedCollection<TValue>>>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, SyncedCollection<TValue>> _dict;
    private readonly Func<IEnumerable<TValue>, ICollection<TValue>> _collectionCreator;

    public ConcurrentMultimap()
        : this(null, null, null)
    {
    }

    public ConcurrentMultimap(IEqualityComparer<TKey> comparer)
        : this(null, comparer, null)
    {
    }

    public ConcurrentMultimap(IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> items)
        : this(items, null, null)
    {
        // for serialization
    }

    public ConcurrentMultimap(
        IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> items,
        IEqualityComparer<TKey> comparer)
        : this(items, comparer, null)
    {
        // for serialization
    }

    public ConcurrentMultimap(
        IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>>? items,
        IEqualityComparer<TKey>? comparer,
        Func<IEnumerable<TValue>, ICollection<TValue>>? collectionCreator)
    {
        _collectionCreator = collectionCreator ?? Multimap<TKey, TValue>.DefaultCollectionCreator;
        _dict = new ConcurrentDictionary<TKey, SyncedCollection<TValue>>(
            ConvertItems(items),
            comparer ?? EqualityComparer<TKey>.Default);
    }

    private IEnumerable<KeyValuePair<TKey, SyncedCollection<TValue>>> ConvertItems(IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>>? items)
    {
        if (items == null)
        {
            yield break;
        }

        foreach (var item in items)
        {
            yield return new KeyValuePair<TKey, SyncedCollection<TValue>>(item.Key, CreateCollection(item.Value));
        }
    }

    protected virtual SyncedCollection<TValue> CreateCollection(IEnumerable<TValue>? values)
    {
        var col = _collectionCreator(values ?? []);
        return col.AsSynchronized();
    }

    /// <summary>
    /// Gets the count of groups/keys.
    /// </summary>
    public int Count
    {
        get => _dict.Count;
    }

    /// <summary>
    /// Gets the total count of items in all groups.
    /// </summary>
    public int TotalValueCount
    {
        get
        {
            var count = 0;

            foreach (var values in _dict.Values)
            {
                count += values.Count;
            }

            return count;
        }
    }

    /// <summary>
    /// Gets the count of items in the requested group.
    /// </summary>
    public int ValueCount(TKey key)
    {
        if (_dict.TryGetValue(key, out var col))
        {
            return col.Count;
        }

        return 0;
    }

    /// <summary>
    /// Gets the synchronized collection of values associated with the specified key.
    /// </summary>
    /// <remarks>This indexer allows for easy access to collections of values based on their keys. It ensures
    /// thread safety when accessing the collection.</remarks>
    /// <param name="key">The key used to access the associated collection of values. Must not be null.</param>
    /// <returns>A synchronized collection of type SyncedCollection<TValue> that contains the values associated with the
    /// specified key. If no collection exists for the key, a new collection is created.</returns>
    public virtual SyncedCollection<TValue> this[TKey key]
    {
        get
        {
            GetOrCreateValues(key, null, out var col);
            return col;
        }
    }

    /// <summary>
    /// Gets the collection of keys.
    /// </summary>
    public virtual ICollection<TKey> Keys
    {
        get => _dict.Keys;
    }

    /// <summary>
    /// Gets all value collections.
    /// </summary>
    public virtual ICollection<SyncedCollection<TValue>> Values
    {
        get => _dict.Values;
    }

    /// <summary>
    /// Attempts to add the specified value to the collection associated with the given key. If the key does not exist,
    /// creates a new collection for the key and adds the value to it.
    /// </summary>
    /// <remarks>If the key already exists, the value is added to the existing collection. This method is
    /// useful for adding values without overwriting existing entries.</remarks>
    /// <param name="key">The key with which the value is to be associated. This parameter cannot be null.</param>
    /// <param name="value">The value to add to the collection for the specified key. This parameter cannot be null.</param>
    public virtual void TryAdd(TKey key, TValue value)
    {
        if (!GetOrCreateValues(key, [value], out var col))
        {
            col.Add(value);
        }
    }

    /// <summary>
    /// Attempts to add the specified values to the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="values">The values.</param>
    public virtual void TryAddRange(TKey key, IEnumerable<TValue> values)
    {
        Guard.NotNull(values);

        if (!GetOrCreateValues(key, values, out var col))
        {
            col.AddRange(values);
        }
    }

    /// <summary>
    /// Attempts to remove the specified value for the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><c>True</c> if such a value existed and was removed; otherwise <c>false</c>.</returns>
    public virtual bool TryRemove(TKey key, TValue value)
    {
        if (!_dict.TryGetValue(key, out var col))
        {
            return false;
        }

        var removed = col.Remove(value);

        if (col.Count == 0)
        {
            _dict.TryRemove(key, out _);
        }

        return removed;
    }

    /// <summary>
    /// Attempts to remove a range of values for the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="values">The values to remove from the group.</param>
    /// <returns><c>True</c> if at least one item in group <paramref name="key"/> has been removed; otherwise <c>false</c>.</returns>
    public virtual bool TryRemoveRange(TKey key, IEnumerable<TValue> values)
    {
        Guard.NotNull(values);

        if (_dict.TryGetValue(key, out var col))
        {
            var numRemoved = col.RemoveRange(values);
            return numRemoved > 0;
        }

        return false;
    }

    /// <summary>
    /// Attempts to remove and return all values for the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><c>True</c> if any such values existed; otherwise <c>false</c>.</returns>
    public virtual bool TryRemoveAll(TKey key, [MaybeNullWhen(false)] out SyncedCollection<TValue> collection)
    {
        return _dict.TryRemove(key, out collection);
    }

    /// <summary>
    /// Removes all values.
    /// </summary>
    public virtual void Clear()
    {
        _dict.Clear();
    }

    /// <summary>
    /// Determines whether the multimap contains any values for the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><c>True</c> if the multimap has one or more values for the specified key, otherwise <c>false</c>.</returns>
    public virtual bool ContainsKey(TKey key)
    {
        return _dict.ContainsKey(key);
    }

    /// <summary>
    /// Determines whether the multimap contains the specified value for the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><c>True</c> if the multimap contains such a value; otherwise, <c>false</c>.</returns>
    public virtual bool ContainsValue(TKey key, TValue value)
    {
        // Must be a single dictionary lookup to avoid race/KeyNotFoundException.
        return _dict.TryGetValue(key, out var col) && col.Contains(value);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the multimap.
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the multimap.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the multimap.
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the multimap.</returns>
    public virtual IEnumerator<KeyValuePair<TKey, SyncedCollection<TValue>>> GetEnumerator()
    {
        return _dict.GetEnumerator();
    }

    private bool GetOrCreateValues(TKey key, IEnumerable<TValue>? initial, out SyncedCollection<TValue> col)
    {
        // Return true when created
        var created = false;

        col = _dict.GetOrAdd(key, _ =>
        {
            created = true;
            return CreateCollection(initial);
        });

        return created;
    }

    #region Static members

    public static ConcurrentMultimap<TKey, TValue> CreateFromLookup(ILookup<TKey, TValue> source)
    {
        Guard.NotNull(source);

        var map = new ConcurrentMultimap<TKey, TValue>();

        foreach (IGrouping<TKey, TValue> group in source)
        {
            map.TryAddRange(group.Key, group);
        }

        return map;
    }

    #endregion
}