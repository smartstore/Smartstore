using System;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// Represents a database query result cache entry.
    /// </summary>
    public class DbCacheEntry
    {
        public DbCacheKey Key { get; set; }
        
        /// <summary>
        /// The cached value, either a single entity or a list of entities.
        /// </summary>
        public object Value { get; set; }
    }
}