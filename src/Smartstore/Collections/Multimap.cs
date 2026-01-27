#nullable enable

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Smartstore.Collections.JsonConverters;
using System.Text.Json.Serialization;

namespace Smartstore.Collections
{
    /// <summary>
    /// A data structure that contains multiple values for each key.
    /// </summary>
    /// <typeparam name="TKey">The type of key.</typeparam>
    /// <typeparam name="TValue">The type of value.</typeparam>
    [JsonConverter(typeof(MultimapJsonConverterFactory))]
    public class Multimap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, ICollection<TValue>>>
        where TKey : notnull
    {
        private readonly IDictionary<TKey, ICollection<TValue>> _dict;
        private readonly Func<IEnumerable<TValue>, ICollection<TValue>>? _collectionCreator;
        private readonly bool _isReadonly = false;

        internal readonly static Func<IEnumerable<TValue>, ICollection<TValue>> DefaultCollectionCreator =
            x => [.. x ?? []];

        public Multimap()
            : this(EqualityComparer<TKey>.Default)
        {
        }

        public Multimap(IEqualityComparer<TKey> comparer)
        {
            _dict = new Dictionary<TKey, ICollection<TValue>>(comparer ?? EqualityComparer<TKey>.Default);
            _collectionCreator = DefaultCollectionCreator;
        }

        public Multimap(Func<IEnumerable<TValue>, ICollection<TValue>> collectionCreator)
            : this(new Dictionary<TKey, ICollection<TValue>>(), collectionCreator)
        {
        }

        public Multimap(IEqualityComparer<TKey> comparer, Func<IEnumerable<TValue>, ICollection<TValue>> collectionCreator)
            : this(new Dictionary<TKey, ICollection<TValue>>(comparer ?? EqualityComparer<TKey>.Default), collectionCreator)
        {
        }

        internal Multimap(IDictionary<TKey, ICollection<TValue>> dictionary, Func<IEnumerable<TValue>, ICollection<TValue>> collectionCreator)
        {
            Guard.NotNull(dictionary);
            Guard.NotNull(collectionCreator);

            _dict = dictionary;
            _collectionCreator = collectionCreator;
        }

        protected Multimap(IDictionary<TKey, ICollection<TValue>> dictionary, bool isReadonly)
        {
            Guard.NotNull(dictionary);

            _dict = dictionary;

            if (isReadonly && dictionary != null)
            {
                foreach (var kvp in dictionary)
                {
                    dictionary[kvp.Key] = kvp.Value.AsReadOnly();
                }
            }

            _isReadonly = isReadonly;
        }

        public Multimap(IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> items)
            : this(items, null)
        {
            // for serialization
        }

        public Multimap(IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> items, IEqualityComparer<TKey>? comparer)
        {
            // for serialization
            Guard.NotNull(items);

            _dict = new Dictionary<TKey, ICollection<TValue>>(comparer ?? EqualityComparer<TKey>.Default);

            if (items != null)
            {
                foreach (var kvp in items)
                {
                    _dict[kvp.Key] = CreateCollection(kvp.Value);
                }
            }
        }

        protected virtual ICollection<TValue> CreateCollection(IEnumerable<TValue>? values)
        {
            return (_collectionCreator ?? DefaultCollectionCreator)(values ?? []);
        }

        /// <summary>
        /// Gets the count of groups/keys.
        /// </summary>
        public int Count
        {
            get => _dict.Keys.Count;
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
        /// Gets the collection of values associated with the specified key. If the key does not exist and the
        /// collection is not read-only, a new collection is created and associated with the key.
        /// </summary>
        /// <remarks>If the key does not exist and the collection is not read-only, a new collection will
        /// be created and associated with the key. The returned collection can be modified unless the instance is
        /// marked as read-only.</remarks>
        /// <param name="key">The key for which to retrieve or create the associated collection of values. Cannot be null.</param>
        /// <returns>A collection of values associated with the specified key. Returns null if the key is not found and the
        /// collection is read-only.</returns>
        public virtual ICollection<TValue>? this[TKey key]
        {
            get
            {
                if (_dict.TryGetValue(key, out var values))
                {
                    return values;
                }

                if (!_isReadonly)
                {
                    values = CreateCollection(null);
                    _dict[key] = values;
                    return values;
                }

                return null;
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
        public virtual ICollection<ICollection<TValue>> Values
        {
            get => _dict.Values;
        }

        /// <summary>
        /// Finds all values that match a predicate within a key group.
        /// </summary>
        /// <param name="key">The key to search in.</param>
        /// <param name="predicate">The predicate to apply.</param>
        /// <returns>An enumerable of matching values; empty if the key does not exist.</returns>
        public IEnumerable<TValue> Find(TKey key, Func<TValue, bool> predicate)
        {
            Guard.NotNull(key);
            Guard.NotNull(predicate);

            if (_dict.TryGetValue(key, out var values))
            {
                return values.Where(predicate);
            }

            return [];
        }

        /// <summary>
        /// Adds the specified value for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public virtual void Add(TKey key, TValue value)
        {
            CheckNotReadonly();

            this[key]!.Add(value);
        }

        /// <summary>
        /// Adds the specified values to the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="values">The values.</param>
        public virtual void AddRange(TKey key, IEnumerable<TValue> values)
        {
            if (values.IsNullOrEmpty())
            {
                return;
            }

            CheckNotReadonly();

            this[key]!.AddRange(values);
        }

        /// <summary>
        /// Removes the specified value for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if such a value existed and was removed; otherwise <c>false</c>.</returns>
        public virtual bool Remove(TKey key, TValue value)
        {
            CheckNotReadonly();

            if (_dict.TryGetValue(key, out var values))
            {
                var removed = values.Remove(value);
                if (removed && values.Count == 0)
                {
                    _dict.Remove(key);
                }

                return removed;
            }

            return false;
        }

        /// <summary>
        /// Removes all values for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>True</c> if any such values existed; otherwise <c>false</c>.</returns>
        public virtual bool RemoveAll(TKey key)
        {
            CheckNotReadonly();
            return _dict.Remove(key);
        }

        /// <summary>
        /// Removes all values.
        /// </summary>
        public virtual void Clear()
        {
            CheckNotReadonly();
            _dict.Clear();
        }

        /// <summary>
        /// Checks whether the multimap contains any values for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>True</c> if the multimap has one or more values for the specified key, otherwise <c>false</c>.</returns>
        public virtual bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        /// <summary>
        /// Gets the values associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="values">The values, if found.</param>
        /// <returns><c>True</c> if the multimap has one or more values for the specified key, otherwise <c>false</c>.</returns>
        public virtual bool TryGetValues(TKey key, [NotNullWhen(true)] out ICollection<TValue>? values)
        {
            return _dict.TryGetValue(key, out values);
        }

        /// <summary>
        /// Determines whether the multimap contains the specified value for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>True</c> if the multimap contains such a value; otherwise, <c>false</c>.</returns>
        public virtual bool ContainsValue(TKey key, TValue value)
        {
            return _dict.TryGetValue(key, out var values) && values.Contains(value);
        }

        /// <summary>
        /// Gets a dictionary view of the collection, where each key is associated with a collection of values.
        /// </summary>
        /// <returns>An IDictionary containing each key and its corresponding collection of values. Modifications to the returned
        /// dictionary affect the underlying collection.</returns>
        public IDictionary<TKey, ICollection<TValue>> AsDictionary()
            => _dict;

        /// <summary>
        /// Returns an enumerator that iterates through the multimap.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the multimap.</returns>
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the multimap.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the multimap.</returns>
        public virtual IEnumerator<KeyValuePair<TKey, ICollection<TValue>>> GetEnumerator()
            => _dict.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckNotReadonly()
        {
            if (_isReadonly)
                throw new NotSupportedException("Multimap is read-only.");
        }

        public static Multimap<TKey, TValue> CreateFromLookup(ILookup<TKey, TValue> source)
        {
            Guard.NotNull(source);

            var map = new Multimap<TKey, TValue>();

            foreach (IGrouping<TKey, TValue> group in source)
            {
                map.AddRange(group.Key, group);
            }
                    
            return map;
        }
    }
}