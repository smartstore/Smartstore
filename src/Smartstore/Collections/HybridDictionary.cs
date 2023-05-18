#nullable enable

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Smartstore.Collections
{
    /// <summary>
    /// HybridDictionary is a dictionary which is implemented to efficiently 
    /// store both small and large numbers of items. When only a single item is stored, 
    /// we use no collections at all. When 1 &lt; n &lt;= MaxListSize is stored, 
    /// we use a list. For any larger number of elements, we use a dictionary.
    /// </summary>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <typeparam name="TValue">The value type</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public class HybridDictionary<TKey, TValue> : 
        IDictionary<TKey, TValue>,
        IReadOnlyDictionary<TKey, TValue>,
        IDictionary where TKey : notnull
    {
        /// <summary>
        /// The maximum number of entries we will store in a list before converting it to a dictionary.
        /// </summary>
        const int MaxListSize = 8;

        /// <summary>
        /// The dictionary, list, or pair used for a store
        /// </summary>
        private object? _store;

        /// <summary>
        /// The comparer used to look up an item.
        /// </summary>
        private readonly IEqualityComparer<TKey> _comparer;

        public HybridDictionary()
            : this(0)
        {
        }

        public HybridDictionary(int capacity)
            : this(capacity, null, true)
        {
        }

        public HybridDictionary(IEqualityComparer<TKey>? comparer)
            : this(0, comparer, true)
        {
        }

        public HybridDictionary(int capacity, IEqualityComparer<TKey>? comparer)
            : this(capacity, comparer, true)
        {
        }

        private HybridDictionary(int capacity, IEqualityComparer<TKey>? comparer, bool createStore)
        {
            Guard.NotNegative(capacity);

            _comparer = comparer ?? EqualityComparer<TKey>.Default;

            if (createStore)
            {
                if (capacity > MaxListSize)
                {
                    _store = new Dictionary<TKey, TValue>(capacity, comparer);
                }
                else if (capacity > 1)
                {
                    _store = new List<KeyValuePair<TKey, TValue>>(capacity);
                }
            }
        }

        public HybridDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
            : this(collection, null)
        {
        }

        public HybridDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer)
            : this(0, comparer, false)
        {
            Guard.NotNull(collection);

            if (!collection.TryGetNonEnumeratedCount(out var count))
            {
                count = collection.Count();
            }

            if (count > 0)
            {
                if (count == 1)
                {
                    var kvp = collection.First();
                    _store = new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value);
                }
                else if (count <= MaxListSize)
                {
                    _store = new List<KeyValuePair<TKey, TValue>>(collection);
                }
                else
                {
                    _store = new Dictionary<TKey, TValue>(collection, comparer);
                }
            }
        }

        /// <summary>
        /// Gets the comparer used to compare keys.
        /// </summary>
        public IEqualityComparer<TKey> Comparer
        {
            get => _comparer;
        }

        /// <inheritdoc />
        ICollection IDictionary.Keys => (ICollection)Keys;

        /// <inheritdoc />
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

        /// <inheritdoc />
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        /// <inheritdoc />
        public ICollection<TKey> Keys
        {
            get
            {
                if (_store is null)
                {
                    return Array.Empty<TKey>();
                }

                if (_store is KeyValuePair<TKey, TValue> kvp)
                {
                    return new TKey[] { kvp.Key };
                }

                if (_store is List<KeyValuePair<TKey, TValue>> list)
                {
                    var keys = new TKey[list.Count];
                    for (int i = 0; i < list.Count; i++)
                    {
                        keys[i] = list[i].Key;
                    }

                    return keys;
                }

                if (_store is Dictionary<TKey, TValue> dict)
                {
                    return dict.Keys;
                }

                return Array.Empty<TKey>();
            }
        }

        /// <inheritdoc />
        ICollection IDictionary.Values => (ICollection)Values;

        /// <inheritdoc />
        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

        /// <inheritdoc />
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        /// <inheritdoc />
        public ICollection<TValue> Values
        {
            get
            {
                if (_store is null)
                {
                    return Array.Empty<TValue>();
                }

                if (_store is KeyValuePair<TKey, TValue> kvp)
                {
                    return new TValue[] { kvp.Value };
                }

                if (_store is List<KeyValuePair<TKey, TValue>> list)
                {
                    var values = new TValue[list.Count];
                    for (int i = 0; i < list.Count; i++)
                    {
                        values[i] = list[i].Value;
                    }

                    return values;
                }

                if (_store is Dictionary<TKey, TValue> dict)
                {
                    return dict.Values;
                }

                return Array.Empty<TValue>();
            }
        }

        /// <inheritdoc />
        public int Count
        {
            get
            {
                if (_store is null)
                {
                    return 0;
                }

                if (_store is KeyValuePair<TKey, TValue>)
                {
                    return 1;
                }

                return ((ICollection)_store).Count;
            }
        }

        /// <inheritdoc />
        public bool IsReadOnly { get; } = false;

        /// <inheritdoc />
        public bool IsSynchronized { get; } = false;

        /// <inheritdoc />
        public bool IsFixedSize { get; } = false;

        /// <inheritdoc />
        public object SyncRoot
        {
            get => this;
        }

        /// <inheritdoc />
        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                {
                    return value;
                }

                return default!;
            }

            set
            {
                TryInsert(key, value, false);
            }
        }

        /// <inheritdoc />
        object? IDictionary.this[object key]
        {
            get => ((IDictionary<TKey, TValue>)this)[(TKey)key];
            set => ((IDictionary<TKey, TValue>)this)[(TKey)key] = (TValue)value!;
        }

        /// <inheritdoc />
        void IDictionary.Add(object key, object? value)
        {
            Add((TKey)key, (TValue)value!);
        }

        /// <inheritdoc />
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <inheritdoc />
        public void Add(TKey key, TValue value)
        {
            TryInsert(key, value, true);
        }

        private bool TryInsert(TKey key, TValue value, bool throwIfPresent)
        {
            Guard.NotNull(key);

            if (_store is null)
            {
                _store = new KeyValuePair<TKey, TValue>(key, value);
                return true;
            }
            else if (_store is KeyValuePair<TKey, TValue> kvp)
            {
                if (_comparer.Equals(kvp.Key, key))
                {
                    if (throwIfPresent)
                    {
                        throw new ArgumentException("A value with the same key already exists in the collection.", nameof(key));
                    }
                    else
                    {
                        _store = new KeyValuePair<TKey, TValue>(key, value);
                    }
                }
                else
                {
                    _store = new List<KeyValuePair<TKey, TValue>>
                    {
                        { kvp },
                        { new KeyValuePair<TKey, TValue>(key, value) }
                    };
                }

                return true;
            }
            else if (_store is List<KeyValuePair<TKey, TValue>> list)
            {
                AddToOrUpdateList(list, key, value, throwIfPresent);
                return true;
            }
            else if (_store is Dictionary<TKey, TValue> dict)
            {
                if (throwIfPresent)
                {
                    dict.Add(key, value);
                }
                else
                {
                    dict[key] = value;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a value to the list, growing it to a dictionary if necessary
        /// </summary>
        private void AddToOrUpdateList(List<KeyValuePair<TKey, TValue>> list, TKey key, TValue value, bool throwIfPresent)
        {
            if (list.Count < MaxListSize) 
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (_comparer.Equals(list[i].Key, key))
                    {
                        if (throwIfPresent)
                        {
                            throw new ArgumentException("A value with the same key already exist in the collection.", nameof(key));
                        }

                        // Update existing item in list
                        list[i] = new KeyValuePair<TKey, TValue>(key, value);
                        return;
                    }
                }

                // Item not in list: add
                list.Add(new KeyValuePair<TKey, TValue>(key, value));
            }
            else
            {
                var dict = new Dictionary<TKey, TValue>(list.Count + 1, _comparer);
                for (var i = 0; i < list.Count; i++)
                {
                    dict.Add(list[i].Key, list[i].Value);
                }

                if (throwIfPresent)
                {
                    dict.Add(key, value);
                }
                else
                {
                    dict[key] = value;
                }

                _store = dict;
            }
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)this).Contains(item) && Remove(item.Key);
        }

        /// <inheritdoc />
        void IDictionary.Remove(object key)
        {
            Remove((TKey)key);
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            Guard.NotNull(key);

            if (_store is null)
            {
                return false;
            }
            else if (_store is KeyValuePair<TKey, TValue> kvp)
            {
                if (_comparer.Equals(kvp.Key, key))
                {
                    // Downgrade
                    _store = null;
                    return true;
                }

                return false;
            }
            else if (_store is List<KeyValuePair<TKey, TValue>> list)
            {
                var removed = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (_comparer.Equals(list[i].Key, key))
                    {
                        list.RemoveAt(i);
                        removed = true;
                        break;
                    }
                }

                // Downgrade if necessary
                if (removed && list.Count == 1)
                {
                    _store = new KeyValuePair<TKey, TValue>(list[0].Key, list[0].Value);
                }

                return removed;
            }
            else if (_store is Dictionary<TKey, TValue> dict)
            {
                if (dict.Remove(key))
                {
                    // Downgrade if necessary
                    if (dict.Count <= MaxListSize)
                    {
                        _store = new List<KeyValuePair<TKey, TValue>>(dict);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            value = default;

            if (_store is null)
            {
                return false;
            }
            else if (_store is KeyValuePair<TKey, TValue> kvp)
            {
                if (_comparer.Equals(kvp.Key, key))
                {
                    value = kvp.Value;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (_store is List<KeyValuePair<TKey, TValue>> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (_comparer.Equals(list[i].Key, key))
                    {
                        value = list[i].Value;
                        return true;
                    }
                }

                return false;
            }
            else if (_store is Dictionary<TKey, TValue> dict)
            {
                return dict.TryGetValue(key, out value);
            }

            return false;
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (_store is ICollection<KeyValuePair<TKey, TValue>> collection)
            {
                collection.Clear();
            }
            
            _store = null;
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            if (_store is null)
            {
                return false;
            }
            else if (_store is ICollection<KeyValuePair<TKey, TValue>> list)
            {
                return list.Contains(item);
            }

            return item.Equals(_store);
        }

        /// <inheritdoc />
        bool IDictionary.Contains(object key)
        {
            return ContainsKey((TKey)key);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(TKey key)
        {
            return TryGetValue(key, out _);
        }

        /// <inheritdoc />
        void ICollection.CopyTo(Array array, int index)
        {
            var i = index;
            foreach (var entry in this)
            {
                array.SetValue(new DictionaryEntry(entry.Key, entry.Value), i);
            }
        }

        /// <inheritdoc />
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            var i = arrayIndex;
            foreach (var entry in this)
            {
                array[i] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new Enumerator(_store);
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator(_store);
        }

        private readonly struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> _inner;

            public Enumerator(object? store)
            {
                if (store is null)
                {
                    _inner = Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
                }
                else if (store is KeyValuePair<TKey, TValue> kvp)
                {
                    _inner = new List<KeyValuePair<TKey, TValue>> { kvp }.GetEnumerator();
                }
                else if (store is List<KeyValuePair<TKey, TValue>> list)
                {
                    _inner = list.GetEnumerator();
                }
                else if (store is Dictionary<TKey, TValue> dict)
                {
                    _inner = dict.GetEnumerator();
                }
                else
                {
                    _inner = Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
                }
            }

            public bool MoveNext() => _inner.MoveNext();
            object? IEnumerator.Current => _inner.Current;
            public KeyValuePair<TKey, TValue> Current => _inner.Current;
            public DictionaryEntry Entry => new(_inner.Current.Key, _inner.Current.Value);
            public object Key => _inner.Current.Key;
            public object? Value => _inner.Current.Value;
            public void Reset() => _inner.Reset();
            public void Dispose() => _inner.Dispose();
        }
    }
}