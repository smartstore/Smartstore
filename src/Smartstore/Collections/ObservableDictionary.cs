using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Smartstore.Collections
{
    /// <summary>
    /// Implementation of a dictionary implementing <see cref="INotifyCollectionChanged"/>
    /// and <see cref="INotifyPropertyChanged"/> to notify listeners
    /// when items get added, removed, updated or the whole dictionary is refreshed.
    /// <para>
    /// For every item added, removed or updated, the <see cref="INotifyPropertyChanged.PropertyChanged"/>
    /// event handler will also be raised (as the key representing the property name). If the value itself
    /// implements <see cref="INotifyPropertyChanged"/> this dictionary will listen to deep property changes
    /// and also raise <see cref="INotifyPropertyChanged.PropertyChanged"/> subsequently.
    /// </para>
    /// </summary>
    public class ObservableDictionary<TKey, TValue>
        : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        enum AppendMode
        {
            Add,
            Replace
        }

        private readonly IDictionary<TKey, TValue> _dict;
        private readonly Dictionary<INotifyPropertyChanged, string> _observerKeyMap = [];

        public ObservableDictionary()
            : this(0, null)
        {
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, null)
        {
        }

        public ObservableDictionary(IEqualityComparer<TKey> comparer)
            : this(0, comparer)
        {
        }

        public ObservableDictionary(int capacity)
            : this(capacity, null)
        {
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            _dict = new Dictionary<TKey, TValue>(dictionary, comparer);
        }

        public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            _dict = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        #region Notifications

        /// <summary>
        /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        public virtual event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Occurs when the collection changes, either by adding or removing an item.
        /// </summary>
        /// <remarks>
        /// see <seealso cref="INotifyCollectionChanged"/>
        /// </remarks>
        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null && !string.IsNullOrEmpty(propertyName))
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> changedItem)
        {
            OnCollectionChanged(
                new NotifyCollectionChangedEventArgs(
                    action: action,
                    changedItem: changedItem));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> newItem, KeyValuePair<TKey, TValue> oldItem)
        {
            OnCollectionChanged(
                new NotifyCollectionChangedEventArgs(
                    action: action,
                    newItem: newItem,
                    oldItem: oldItem));
        }

        private void OnCollectionReset()
        {
            OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
        }

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Watches for deep changes in value types that implement <see cref="INotifyPropertyChanged"/>
        /// and raises the local <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        public void StartObserveValues()
        {
            foreach (var kvp in _dict)
            {
                StartObserve(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Stops watching for deep changes in value types that implement <see cref="INotifyPropertyChanged"/>.
        /// </summary>
        public void StopObserveValues()
        {
            foreach (var kvp in _dict)
            {
                StopObserve(kvp.Key, kvp.Value);
            }
        }

        private void StartObserve(TKey key, TValue value)
        {
            if (key is string strKey && value is INotifyPropertyChanged notifyPropChanged)
            {
                if (!_observerKeyMap.ContainsKey(notifyPropChanged))
                {
                    _observerKeyMap[notifyPropChanged] = strKey;
                    notifyPropChanged.PropertyChanged += OnValuePropertyChanged;
                }
            }
        }

        private void StopObserve(TKey key, TValue value)
        {
            if (key is string strKey && value is INotifyPropertyChanged notifyPropChanged)
            {
                if (_observerKeyMap.Remove(notifyPropChanged))
                {
                    notifyPropChanged.PropertyChanged -= OnValuePropertyChanged;
                }
            }
        }

        private void OnValuePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Watch for deep changes
            if (sender is INotifyPropertyChanged notifyPropChanged)
            {
                if (_observerKeyMap.TryGetValue(notifyPropChanged, out var key))
                {
                    OnPropertyChanged(key);
                }
            }
        }

        #endregion

        #region IDictionary<TKey, TValue>

        /// <inheritdoc/>
        public TValue this[TKey key]
        {
            get
            {
                return _dict[key];
            }
            set
            {
                SetItem(key, value, appendMode: AppendMode.Replace);
            }
        }

        /// <inheritdoc/>
        public ICollection<TKey> Keys
            => _dict.Keys;

        /// <inheritdoc/>
        public ICollection<TValue> Values
            => _dict.Values;

        /// <inheritdoc/>
        public int Count
            => _dict.Count;

        /// <inheritdoc/>
        public bool IsReadOnly
            => _dict.IsReadOnly;

        /// <inheritdoc/>
        public void Add(TKey key, TValue value)
        {
            SetItem(key, value, appendMode: AppendMode.Add);
        }

        /// <inheritdoc/>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            SetItem(item.Key, item.Value, appendMode: AppendMode.Add);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            if (!_dict.Any())
            {
                return;
            }

            var removedItems = new List<KeyValuePair<TKey, TValue>>(_dict.AsEnumerable());
            _dict.Clear();

            if (removedItems.Count > 0)
            {
                OnPropertyChanged(EventArgsCache.CountPropertyChanged);
                OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);
                OnPropertyChanged(EventArgsCache.KeysPropertyChanged);
                OnPropertyChanged(EventArgsCache.ValuesPropertyChanged);
                OnCollectionReset();

                if (typeof(TKey) == typeof(string) && PropertyChanged != null)
                {
                    foreach (var kvp in removedItems)
                    {
                        StopObserve(kvp.Key, kvp.Value);
                        OnPropertyChanged(new PropertyChangedEventArgs(kvp.Key as string));
                    }
                }
            }
        }

        /// <inheritdoc/>
        public bool Contains(KeyValuePair<TKey, TValue> item)
            => _dict.Contains(item);

        /// <inheritdoc/>
        public bool ContainsKey(TKey key)
            => _dict.ContainsKey(key);

        /// <inheritdoc/>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _dict.CopyTo(
                array: array,
                arrayIndex: arrayIndex);
        }

        /// <inheritdoc/>
        public bool Remove(TKey key)
        {
            if (_dict.Remove(key, out var value))
            {
                OnRemove(new KeyValuePair<TKey, TValue>(key, value));
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (_dict.Remove(item))
            {
                OnRemove(item);
                return true;
            }

            return false;
        }

        private void OnRemove(KeyValuePair<TKey, TValue> item)
        {
            StopObserve(item.Key, item.Value);
            OnPropertyChanged(EventArgsCache.CountPropertyChanged);
            OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);
            OnPropertyChanged(EventArgsCache.KeysPropertyChanged);
            OnPropertyChanged(EventArgsCache.ValuesPropertyChanged);
            OnPropertyChanged(item.Key as string);
            OnCollectionChanged(
                action: NotifyCollectionChangedAction.Remove,
                changedItem: item);
        }

        /// <inheritdoc/>
        public bool TryGetValue(TKey key, out TValue value)
            => _dict.TryGetValue(key, out value);

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
            => _dict.GetEnumerator();

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => _dict.GetEnumerator();

        #endregion

        #region IReadOnlyDictionary

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
            => _dict.Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
            => _dict.Values;

        #endregion

        #region ObservableDictionary inner methods

        private void SetItem(TKey key, TValue value, AppendMode appendMode)
        {
            Guard.NotNull(key, nameof(key));

            if (!_dict.TryGetValue(key, out var oldValue))
            {
                _dict[key] = value;

                OnPropertyChanged(EventArgsCache.CountPropertyChanged);
                OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);
                OnPropertyChanged(EventArgsCache.KeysPropertyChanged);
                OnPropertyChanged(EventArgsCache.ValuesPropertyChanged);
                OnPropertyChanged(key as string);
                OnCollectionChanged(
                    action: NotifyCollectionChangedAction.Add,
                    changedItem: new KeyValuePair<TKey, TValue>(key, value));

                StartObserve(key, value);
            }
            else
            {
                if (appendMode == AppendMode.Add)
                {
                    throw new ArgumentException($"Item with the key '{key}' has already been added.");
                }

                if (!Equals(oldValue, value))
                {
                    StopObserve(key, oldValue);

                    _dict[key] = value;

                    OnPropertyChanged(EventArgsCache.ValuesPropertyChanged);
                    OnPropertyChanged(key as string);
                    OnCollectionChanged(
                        action: NotifyCollectionChangedAction.Replace,
                        newItem: new KeyValuePair<TKey, TValue>(key, value),
                        oldItem: new KeyValuePair<TKey, TValue>(key, oldValue));

                    StartObserve(key, value);
                }
            }
        }

        #endregion

        static class EventArgsCache
        {
            internal static readonly PropertyChangedEventArgs CountPropertyChanged = new("Count");
            internal static readonly PropertyChangedEventArgs KeysPropertyChanged = new("Keys");
            internal static readonly PropertyChangedEventArgs ValuesPropertyChanged = new("Values");
            internal static readonly PropertyChangedEventArgs IndexerPropertyChanged = new("Item[]");
            internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new(NotifyCollectionChangedAction.Reset);
        }
    }
}
