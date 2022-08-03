using System.Collections;
using Newtonsoft.Json;
using Smartstore.Collections.JsonConverters;

namespace Smartstore.Collections
{
    /// <summary>
    /// A data structure that contains multiple values for each key.
    /// </summary>
    /// <typeparam name="TKey">The type of key.</typeparam>
    /// <typeparam name="TValue">The type of value.</typeparam>
    [JsonConverter(typeof(MultiMapJsonConverter))]
    public class Multimap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, ICollection<TValue>>>
    {
        private readonly IDictionary<TKey, ICollection<TValue>> _dict;
        private readonly Func<IEnumerable<TValue>, ICollection<TValue>> _collectionCreator;
        private readonly bool _isReadonly = false;

        internal readonly static Func<IEnumerable<TValue>, ICollection<TValue>> DefaultCollectionCreator =
            x => new List<TValue>(x ?? Enumerable.Empty<TValue>());

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
            Guard.NotNull(dictionary, nameof(dictionary));
            Guard.NotNull(collectionCreator, nameof(collectionCreator));

            _dict = dictionary;
            _collectionCreator = collectionCreator;
        }

        protected Multimap(IDictionary<TKey, ICollection<TValue>> dictionary, bool isReadonly)
        {
            Guard.NotNull(dictionary, nameof(dictionary));

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

        public Multimap(IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> items, IEqualityComparer<TKey> comparer)
        {
            // for serialization
            Guard.NotNull(items, nameof(items));

            _dict = new Dictionary<TKey, ICollection<TValue>>(comparer ?? EqualityComparer<TKey>.Default);

            if (items != null)
            {
                foreach (var kvp in items)
                {
                    _dict[kvp.Key] = CreateCollection(kvp.Value);
                }
            }
        }

        protected virtual ICollection<TValue> CreateCollection(IEnumerable<TValue> values)
        {
            return (_collectionCreator ?? DefaultCollectionCreator)(values ?? Enumerable.Empty<TValue>());
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
            get => _dict.Values.Sum(x => x.Count);
        }

        /// <summary>
        /// Gets the collection of values stored under the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
		public virtual ICollection<TValue> this[TKey key]
        {
            get
            {
                if (!_dict.ContainsKey(key))
                {
                    if (!_isReadonly)
                    {
                        _dict[key] = CreateCollection(null);
                    }
                    else
                    {
                        return null;
                    }
                }

                return _dict[key];
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

        public IEnumerable<TValue> Find(TKey key, Func<TValue, bool> predicate)
        {
            Guard.NotNull(key, nameof(key));
            Guard.NotNull(predicate, nameof(predicate));

            if (_dict.TryGetValue(key, out var values))
            {
                return values.Where(predicate);
            }

            return Enumerable.Empty<TValue>();
        }

        /// <summary>
        /// Adds the specified value for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public virtual void Add(TKey key, TValue value)
        {
            CheckNotReadonly();

            this[key].Add(value);
        }

        /// <summary>
        /// Adds the specified values to the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="values">The values.</param>
        public virtual void AddRange(TKey key, IEnumerable<TValue> values)
        {
            if (values == null || !values.Any())
            {
                return;
            }

            CheckNotReadonly();

            this[key].AddRange(values);
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
        /// <returns><c>True</c> if the multimap has one or more values for the specified key, otherwise <c>false</c>.</returns>
        public virtual bool TryGetValues(TKey key, out ICollection<TValue> values)
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

        public IDictionary<TKey, ICollection<TValue>> AsDictionary()
            => _dict;

        /// <summary>
        /// Returns an enumerator that iterates through the multimap.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the multimap.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the multimap.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the multimap.</returns>
		public virtual IEnumerator<KeyValuePair<TKey, ICollection<TValue>>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        private void CheckNotReadonly()
        {
            if (_isReadonly)
                throw new NotSupportedException("Multimap is read-only.");
        }

        public static Multimap<TKey, TValue> CreateFromLookup(ILookup<TKey, TValue> source)
        {
            Guard.NotNull(source, nameof(source));

            var map = new Multimap<TKey, TValue>();

            foreach (IGrouping<TKey, TValue> group in source)
            {
                map.AddRange(group.Key, group);
            }

            return map;
        }

        #region Backlog

        //class GroupingIterator : IEnumerator<IGrouping<TKey, TValue>>
        //{
        //    private readonly IEnumerator<KeyValuePair<TKey, ICollection<TValue>>> _inner;

        //    public GroupingIterator(IEnumerator<KeyValuePair<TKey, ICollection<TValue>>> inner)
        //    {
        //        _inner = inner;
        //    }

        //    public IGrouping<TKey, TValue> Current => new GroupingWrapper(_inner.Current);
        //    object IEnumerator.Current => new GroupingWrapper(_inner.Current);

        //    public bool MoveNext() => _inner.MoveNext();
        //    public void Reset() => _inner.Reset();
        //    public void Dispose() => _inner.Dispose();
        //}

        //class GroupingWrapper : IGrouping<TKey, TValue>
        //{
        //    private readonly KeyValuePair<TKey, ICollection<TValue>> _inner;

        //    public GroupingWrapper(KeyValuePair<TKey, ICollection<TValue>> inner)
        //    {
        //        _inner = inner;
        //    }

        //    public TKey Key => _inner.Key;
        //    public IEnumerator<TValue> GetEnumerator() => _inner.Value.GetEnumerator();
        //    IEnumerator IEnumerable.GetEnumerator() => _inner.Value.GetEnumerator();
        //}

        #endregion
    }
}