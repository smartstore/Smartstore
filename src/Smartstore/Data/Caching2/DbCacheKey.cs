using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Data.Caching2
{
    /// <summary>
    /// Stores information of the computed key of the input LINQ query.
    /// </summary>
    public sealed class DbCacheKey
    {
        /// <summary>
        /// The computed key of the input LINQ query.
        /// </summary>
        public string Key { set; get; }

        /// <summary>
        /// Hash of the input LINQ query's computed key.
        /// </summary>
        public string KeyHash { set; get; }

        /// <summary>
        /// Determines which entities are used in this LINQ query.
        /// This array will be used to invalidate the related cache of all related queries automatically.
        /// </summary>
        public ISet<string> CacheDependencies { set; get; } = new HashSet<string>();

        public override bool Equals(object obj)
        {
            if (obj is not DbCacheKey efCacheKey)
                return false;

            return this.KeyHash == efCacheKey.KeyHash;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                return (hash * 23) + KeyHash.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"KeyHash: {KeyHash}, CacheDependencies: {string.Join(", ", CacheDependencies)}.";
        }
    }
}
