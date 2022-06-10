using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Smartstore.Collections
{
    internal enum AppendMode
    {
        Add,
        Replace
    }

    public class ObservableDictionary<TKey, TValue> 
        : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private const string CountString = nameof(ICollection.Count);
        private const string IndexerName = "Item[]";
        private const string KeysName = "Keys";
        private const string ValuesName = "Values";

        private readonly IDictionary<TKey, TValue> _dict;

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

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        #region IDictionary<TKey, TValue>

        public TValue this[TKey key]
        {
            get
            {
                return _dict[key];
            }
            set
            {
                if (InsertItem(
                    key: key,
                    value: value,
                    appendMode: AppendMode.Replace,
                    out var oldValue))
                {
                    if (oldValue != null)
                    {
                        OnCollectionChanged(
                            action: NotifyCollectionChangedAction.Replace,
                            newItem: new KeyValuePair<TKey, TValue>(key, value),
                            oldItem: new KeyValuePair<TKey, TValue>(key, oldValue));
                    }
                    else
                    {
                        OnCollectionChanged(
                            action: NotifyCollectionChangedAction.Add,
                            changedItem: new KeyValuePair<TKey, TValue>(key, value));
                    }
                };
            }
        }

        public ICollection<TKey> Keys 
            => _dict.Keys;

        public ICollection<TValue> Values 
            => _dict.Values;

        public int Count 
            => _dict.Count;

        public bool IsReadOnly
            => _dict.IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            if (InsertItem(
                key: key,
                value: value,
                appendMode: AppendMode.Add))
            {
                OnCollectionChanged(
                    action: NotifyCollectionChangedAction.Add,
                    changedItem: new KeyValuePair<TKey, TValue>(key, value));
            };
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            if (InsertItem(
                key: item.Key,
                value: item.Value,
                appendMode: AppendMode.Add))
            {
                OnCollectionChanged(
                    action: NotifyCollectionChangedAction.Add,
                    changedItem: new KeyValuePair<TKey, TValue>(item.Key, item.Value));
            };
        }

        public void Clear()
        {
            if (!_dict.Any())
            {
                return;
            }

            var removedItems = new List<KeyValuePair<TKey, TValue>>(_dict.AsEnumerable());
            _dict.Clear();

            OnCollectionChanged(
                action: NotifyCollectionChangedAction.Reset,
                newItems: null,
                oldItems: removedItems);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
            => _dict.Contains(item);

        public bool ContainsKey(TKey key)
            => _dict.ContainsKey(key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _dict.CopyTo(
                array: array,
                arrayIndex: arrayIndex);
        }

        public bool Remove(TKey key)
        {
            if (_dict.Remove(key, out var value))
            {
                OnCollectionChanged(
                    action: NotifyCollectionChangedAction.Remove,
                    changedItem: new KeyValuePair<TKey, TValue>(key, value));

                return true;
            }

            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (_dict.Remove(item))
            {
                OnCollectionChanged(
                    action: NotifyCollectionChangedAction.Remove,
                    changedItem: item);

                return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
            => _dict.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator()
            => _dict.GetEnumerator();

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

        private bool InsertItem(TKey key, TValue value, AppendMode appendMode)
            => InsertItem(key, value, appendMode, out _);

        private bool InsertItem(TKey key, TValue value, AppendMode appendMode, out TValue oldValue)
        {
            Guard.NotNull(key, nameof(key));
            
            oldValue = default;

            if (_dict.TryGetValue(key, out var item))
            {
                if (appendMode == AppendMode.Add)
                {
                    throw new ArgumentException($"Item with the key '{key}' has already been added.");
                }

                if (Equals(item, value))
                {
                    return false;
                }

                _dict[key] = value;
                oldValue = item;
                return true;
            }
            else
            {
                _dict[key] = value;
                return true;
            }
        }

        private void OnPropertyChanged()
        {
            OnPropertyChanged(CountString);
            OnPropertyChanged(IndexerName);
            OnPropertyChanged(KeysName);
            OnPropertyChanged(ValuesName);
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                OnPropertyChanged();
            }

            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnCollectionChanged()
        {
            OnPropertyChanged();
            var handler = CollectionChanged;
            handler?.Invoke(
                this, new NotifyCollectionChangedEventArgs(
                    action: NotifyCollectionChangedAction.Reset));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> changedItem)
        {
            OnPropertyChanged();
            var handler = CollectionChanged;
            handler?.Invoke(
                this, new NotifyCollectionChangedEventArgs(
                    action: action,
                    changedItem: changedItem));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> newItem, KeyValuePair<TKey, TValue> oldItem)
        {
            OnPropertyChanged();
            var handler = CollectionChanged;
            handler?.Invoke(
                this, new NotifyCollectionChangedEventArgs(
                    action: action,
                    newItem: newItem,
                    oldItem: oldItem));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, IList newItems)
        {
            OnPropertyChanged();
            var handler = CollectionChanged;
            handler?.Invoke(
                this, new NotifyCollectionChangedEventArgs(
                    action: action,
                    changedItems: newItems));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, IList newItems, IList oldItems)
        {
            OnPropertyChanged();
            var handler = CollectionChanged;
            handler?.Invoke(
                this, new NotifyCollectionChangedEventArgs(
                    action: action,
                    newItems: newItems,
                    oldItems: oldItems));
        }

        #endregion
    }
}
