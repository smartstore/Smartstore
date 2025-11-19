#nullable enable

using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Smartstore.Core.AI.Metadata
{
    public class AIModelCollection : ICollection<AIModelEntry>
    {
        protected readonly KeyedCollection<string, AIModelEntry> _innerCollection;

        public AIModelCollection()
            : this(null)
        {
        }

        public AIModelCollection(IEnumerable<AIModelEntry>? entries)
        {
            _innerCollection = new AIModelKeyedCollection(entries);
        }

        public bool TryFindModel(string? id, [NotNullWhen(true)] out AIModelEntry? entry)
        {
            entry = null;

            if (id != null && _innerCollection.TryGetValue(id, out entry))
            {
                return true;
            }

            return false;
        }

        public AIModelEntry? FindModel(string? id)
        {
            if (TryFindModel(id, out var entry))
            {
                return entry;
            }

            return null;
        }

        public void ChangeModelId(AIModelEntry entry, string newId)
        {
            Guard.NotNull(entry);
            Guard.NotEmpty(newId);

            if (!entry.Id.EqualsNoCase(newId))
            {
                ((AIModelKeyedCollection)_innerCollection).ChangeEntryKey(entry, newId);
                entry.Id = newId;
            }
        }

        #region ICollection<RuleDescriptor>

        public int Count 
            => _innerCollection.Count;

        public bool IsReadOnly 
            => false;

        public void Add(AIModelEntry item)
            => _innerCollection.Add(item);

        public void Clear()
            => _innerCollection.Clear();

        public bool ContainsKey(string key)
            => _innerCollection.Contains(key);

        public bool Contains(AIModelEntry item)
            => _innerCollection.Contains(item.Id);

        public void CopyTo(AIModelEntry[] array, int arrayIndex)
            => _innerCollection.CopyTo(array, arrayIndex);

        public bool Remove(AIModelEntry item)
            => _innerCollection.Remove(item.Id);

        public IEnumerator<AIModelEntry> GetEnumerator()
            => _innerCollection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        #endregion

        #region AIModelKeyedCollection

        class AIModelKeyedCollection : KeyedCollection<string, AIModelEntry>
        {
            public AIModelKeyedCollection(IEnumerable<AIModelEntry>? entries)
                : base(StringComparer.OrdinalIgnoreCase)
            {
                entries?.Each(Add);
            }

            protected override string GetKeyForItem(AIModelEntry item)
            {
                return item.Id;
            }

            public void ChangeEntryKey(AIModelEntry entry, string newKey)
            {
                ChangeItemKey(entry, newKey);
            }
        }

        #endregion
    }
}
