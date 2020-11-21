using System;
using Smartstore.Caching;
using Newtonsoft.Json;
using J = System.Text.Json;
using Smartstore.ComponentModel;
using Smartstore.Data.Caching.JsonConverters;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// Represents a database query result cache entry.
    /// </summary>
    [JsonConverter(typeof(ObjectWrapperJsonConverter))]
    [J.Serialization.JsonConverter(typeof(DbCacheEntryJsonConverter))]
    public class DbCacheEntry : IObjectContainer
    {
        public DbCacheKey Key { get; set; }

        /// <summary>
        /// Gets the type of the cache entry value.
        /// <para>Might be useful for (de)serialization.</para>
        /// </summary>
        public Type ValueType { get; set; }

        /// <summary>
        /// The cached value, either a single entity or a list of entities.
        /// </summary>
        public object Value { get; set; }
    }
}