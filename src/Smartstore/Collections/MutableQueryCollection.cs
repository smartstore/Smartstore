using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Smartstore.Collections
{
    public class MutableQueryCollection : QueryCollection
    {
        private Dictionary<string, StringValues> _store;

        public MutableQueryCollection()
            : this(new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase))
        {
        }

        public MutableQueryCollection(QueryString queryString)
            : this(queryString.ToString())
        {
        }

        public MutableQueryCollection(string queryString)
            : this(QueryHelpers.ParseNullableQuery(queryString))
        {
        }

        public MutableQueryCollection(Dictionary<string, StringValues> store)
            : base(store)
        {
            _store = store;
        }

        /// <summary>
        /// Adds a name value pair to the collection.
        /// </summary>
        /// <param name="name">the name</param>
        /// <param name="value">the value associated to the name</param>
        /// <param name="isUnique">true if the name is unique within the querystring. This allows us to override existing values</param>
        public virtual MutableQueryCollection Add(string name, string value, bool isUnique = false)
        {
            Guard.NotEmpty(name, nameof(name));
            
            if (_store.TryGetValue(name, out var existingValues))
            {
                var passedValues = new StringValues(value.SplitSafe(",").Select(x => x.ToString()).ToArray());
                _store[name] = isUnique ? passedValues : StringValues.Concat(existingValues, passedValues);
            }
            else
            {
                _store.Add(name, value);
            }

            return this;
        }

        /// <summary>
        /// Removes a name value pair from the collection
        /// </summary>
        /// <param name="name">name of the querystring value to remove</param>
        public MutableQueryCollection Remove(string name)
        {
            Guard.NotEmpty(name, nameof(name));
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
            return QueryString.Create(_store);
        }

        public override string ToString()
        {
            return ToQueryString().Value;
        }
    }
}