using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Smartstore.Collections
{
    public class MutableQueryCollection : QueryCollection
    {
        private readonly Dictionary<string, StringValues> _store;

        public MutableQueryCollection()
            : this(new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase))
        {
        }

        public MutableQueryCollection(QueryString queryString)
            : this(queryString.ToString())
        {
        }

        public MutableQueryCollection(string queryString)
            : this(QueryHelpers.ParseQuery(queryString))
        {
        }

        public MutableQueryCollection(Dictionary<string, StringValues> store)
            : base(store)
        {
            Guard.NotNull(store);

            _store = store;
        }

        public Dictionary<string, StringValues> Store
        {
            get => _store;
        }

        /// <summary>
        /// Adds a name value pair to the collection.
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="value">The value associated with the name</param>
        /// <param name="isUnique">true if the name is unique within the querystring. This allows us to override existing values</param>
        public virtual MutableQueryCollection Add(string name, string value, bool isUnique = false)
        {
            Guard.NotEmpty(name);
           
            if (_store.TryGetValue(name, out var existingValues))
            {
                var passedValues = new StringValues(value.SplitSafe(',').ToArray());
                _store[name] = isUnique ? passedValues : StringValues.Concat(existingValues, passedValues);
            }
            else
            {
                _store.Add(name, value);
            }

            return this;
        }

        /// <summary>
        /// Adds a name value pair to the collection.
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="values">The value associated with the name</param>
        /// <param name="isUnique">true if the name is unique within the querystring. This allows us to override existing values</param>
        public virtual MutableQueryCollection Add(string name, StringValues values, bool isUnique = false)
        {
            Guard.NotEmpty(name);

            if (_store.TryGetValue(name, out var existingValues))
            {
                _store[name] = isUnique ? values : StringValues.Concat(existingValues, values);
            }
            else
            {
                _store.Add(name, values);
            }

            return this;
        }

        /// <summary>
        /// Removes a name value pair from the collection
        /// </summary>
        /// <param name="name">name of the querystring value to remove</param>
        public MutableQueryCollection Remove(string name)
        {
            Guard.NotEmpty(name);
            _store.TryRemove(name, out _);
            return this;
        }

        /// <summary>
        /// Clears the collection
        /// </summary>
        public MutableQueryCollection Clear()
        {
            _store.Clear();
            return this;
        }

        public QueryString ToQueryString()
        {
            // INFO: parameters with the same name\key are not comma-separated combined by QueryString.
            // Not even if you pass the values via StringValues.
            return QueryString.Create(_store);
        }

        public override string ToString()
        {
            return ToQueryString().Value;
        }
    }
}