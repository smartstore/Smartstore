using System;

namespace Smartstore.Data.Caching2
{
    /// <summary>
    /// Represents a database query result cache entry.
    /// </summary>
    public class DbCacheEntry
    {
        /// <summary>
        /// The cached value, either a single entity or a list of entities.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// The dependant entity sets.
        /// </summary>
        public string[] EntitySets { get; set; }
    }
}