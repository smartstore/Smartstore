using System.Collections;
using Smartstore.Collections;

namespace Smartstore.Core.Search.Indexing
{
    public class IndexDocument : IIndexDocument
    {
        protected readonly Multimap<string, IndexField> _fields = new(StringComparer.OrdinalIgnoreCase);

        public IndexDocument(int id)
            : this(id, null)
        {
        }

        public IndexDocument(int id, string documentType = null)
        {
            Add(new IndexField("id", id).Store());

            if (documentType.HasValue())
            {
                Add(new IndexField("doctype", documentType).Store());
            }
        }

        public int Count => _fields.TotalValueCount;

        public int Id => (int)_fields["id"].FirstOrDefault().Value;

        public virtual string DocumentType
        {
            get
            {
                if (_fields.ContainsKey("doctype"))
                {
                    return _fields["doctype"].FirstOrDefault().Value as string;
                }

                return null;
            }
        }

        public virtual void Add(IndexField field)
        {
            if (field.Name.EqualsNoCase("id") && _fields.ContainsKey("id"))
            {
                // Special treatment for id and doctype field: allow only one!
                _fields.RemoveAll("id");
            }

            if (field.Name.EqualsNoCase("doctype") && _fields.ContainsKey("doctype"))
            {
                _fields.RemoveAll("doctype");
            }

            _fields.Add(field.Name, field);
        }

        public int Remove(string name)
        {
            if (_fields.ContainsKey(name))
            {
                var num = _fields[name].Count;
                _fields.RemoveAll(name);
                return num;
            }

            return 0;
        }

        public bool Contains(string name)
            => _fields.ContainsKey(name);

        public IEnumerable<IndexField> this[string name]
            => _fields.ContainsKey(name) ? _fields[name] : Enumerable.Empty<IndexField>();

        public IEnumerator<IndexField> GetEnumerator()
            => _fields.SelectMany(x => x.Value).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
