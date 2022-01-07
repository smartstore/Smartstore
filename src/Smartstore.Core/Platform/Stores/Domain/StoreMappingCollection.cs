using System.Collections;

namespace Smartstore.Core.Stores
{
    public class StoreMappingCollection : IReadOnlyCollection<StoreMapping>
    {
        private readonly string _entityName;
        private readonly IDictionary<string, StoreMapping> _dict;
        private HashSet<int> _requestedSet;

        public StoreMappingCollection(string entityName, int[] requestedSet, IEnumerable<StoreMapping> items)
        {
            Guard.NotEmpty(entityName, nameof(entityName));
            Guard.NotNull(items, nameof(items));

            _entityName = entityName;
            _dict = items.ToDictionarySafe(x => CreateKey(x.EntityId, x.StoreId));

            if (requestedSet != null && requestedSet.Length > 0)
            {
                _requestedSet = new HashSet<int>(requestedSet);
            }
        }

        public void MergeWith(StoreMappingCollection other)
        {
            Guard.NotNull(other, nameof(other));

            if (!_entityName.EqualsNoCase(other._entityName))
            {
                throw new InvalidOperationException($"Expected group '{_entityName}', but was '{other._entityName}'");
            }

            // Merge dictionary.
            other._dict.Merge(_dict, true);

            // Merge requested set (entity ids).
            if (_requestedSet != null)
            {
                if (other._requestedSet == null)
                {
                    other._requestedSet = new HashSet<int>(_requestedSet);
                }
                else
                {
                    other._requestedSet.AddRange(_requestedSet);
                }
            }
        }

        public StoreMapping Find(int entityId, int storeId)
        {
            if (_dict.TryGetValue(CreateKey(entityId, storeId), out var mapping))
            {
                return mapping;
            }

            return default;
        }

        public int Count => _dict.Values.Count;

        private static string CreateKey(int entityId, int storeId)
        {
            return entityId + "-" + storeId;
        }

        public IEnumerator<StoreMapping> GetEnumerator()
        {
            return _dict.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dict.Values.GetEnumerator();
        }
    }
}
