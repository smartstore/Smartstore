using System.Collections;
using System.Collections.ObjectModel;

namespace Smartstore.Core.Rules
{
    public class RuleDescriptorCollection : ICollection<RuleDescriptor>
    {
        protected readonly KeyedCollection<string, RuleDescriptor> _innerCollection;

        public RuleDescriptorCollection()
            : this(null)
        {
        }

        public RuleDescriptorCollection(IEnumerable<RuleDescriptor> descriptors)
        {
            _innerCollection = new RuleDescriptorKeyedCollection(descriptors);
        }

        public virtual RuleDescriptor FindDescriptor(string name)
        {
            if (name != null && _innerCollection.TryGetValue(name, out var descriptor))
            {
                return descriptor;
            }

            return null;
        }

        #region ICollection<RuleDescriptor>

        public int Count => _innerCollection.Count;

        public bool IsReadOnly => false;

        public void Add(RuleDescriptor item)
        {
            _innerCollection.Add(item);
        }

        public void Clear()
        {
            _innerCollection.Clear();
        }

        public bool Contains(RuleDescriptor item)
        {
            return _innerCollection.Contains(item.Name);
        }

        public void CopyTo(RuleDescriptor[] array, int arrayIndex)
        {
            _innerCollection.CopyTo(array, arrayIndex);
        }

        public bool Remove(RuleDescriptor item)
        {
            return _innerCollection.Remove(item.Name);
        }

        public virtual IEnumerator<RuleDescriptor> GetEnumerator()
        {
            return _innerCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region RuleDescriptorKeyedCollection

        class RuleDescriptorKeyedCollection : KeyedCollection<string, RuleDescriptor>
        {
            public RuleDescriptorKeyedCollection(IEnumerable<RuleDescriptor> descriptors)
                : base(StringComparer.OrdinalIgnoreCase)
            {
                descriptors?.Each(Add);
            }

            protected override string GetKeyForItem(RuleDescriptor item)
            {
                return item.Name;
            }
        }

        #endregion
    }
}
