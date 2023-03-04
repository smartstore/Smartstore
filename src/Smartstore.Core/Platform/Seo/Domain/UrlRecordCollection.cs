using System.Collections;

namespace Smartstore.Core.Seo
{
    public class UrlRecordCollection : IReadOnlyCollection<UrlRecord>
    {
        private readonly string _entityName;
        private readonly IDictionary<string, UrlRecord> _dict;
        private HashSet<int> _requestedSet;

        public UrlRecordCollection(string entityName, int[] requestedSet, IEnumerable<UrlRecord> items)
        {
            Guard.NotEmpty(entityName);
            Guard.NotNull(items);

            _entityName = entityName;
            _dict = items.ToDictionarySafe(x => CreateKey(x.EntityId, x.LanguageId));

            if (requestedSet != null && requestedSet.Length > 0)
            {
                _requestedSet = new HashSet<int>(requestedSet);
            }
        }

        public void MergeWith(UrlRecordCollection other)
        {
            Guard.NotNull(other);

            if (!_entityName.EqualsNoCase(other._entityName))
            {
                throw new InvalidOperationException("Expected group '{0}', but was '{1}'".FormatInvariant(_entityName, other._entityName));
            }

            // Merge dictionary
            other._dict.Merge(_dict, true);

            // Merge requested set (entity ids)
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

        public string GetSlug(int languageId, int entityId, bool returnDefaultValue = true)
        {
            var slug = Find(languageId, entityId)?.Slug;

            if (returnDefaultValue && languageId != 0 && string.IsNullOrEmpty(slug))
            {
                slug = Find(0, entityId)?.Slug;
            }

            return slug;
        }

        public UrlRecord Find(int languageId, int entityId)
        {
            var item = _dict.Get(CreateKey(entityId, languageId));

            if (item == null && (_requestedSet == null || _requestedSet.Contains(entityId)))
            {
                // Although the item does not exist in the local dictionary it has been requested
                // from the database, which means it does not exist in the db either.
                // Avoid the upcoming roundtrip.
                return new UrlRecord
                {
                    EntityName = _entityName,
                    EntityId = entityId,
                    LanguageId = languageId
                };
            }

            return item;
        }

        private static string CreateKey(int entityId, int languageId)
        {
            return string.Concat(entityId.ToStringInvariant(), "-", languageId);
        }

        public int Count => _dict.Values.Count;
        public IEnumerator<UrlRecord> GetEnumerator()
        {
            return _dict.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dict.Values.GetEnumerator();
        }
    }
}
