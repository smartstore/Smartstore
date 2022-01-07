namespace Smartstore.Data.Caching
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
        /// Determines which entities are involved in the LINQ query (by JOIN, INCLUDE etc.).
        /// This array will be used to invalidate data of all related queries automatically.
        /// </summary>
        public string[] EntitySets { set; get; } = Array.Empty<string>();

        public override bool Equals(object obj)
        {
            if (obj is not DbCacheKey efCacheKey)
                return false;

            return this.Key == efCacheKey.Key;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                return (hash * 23) + Key.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"KeyHash: {Key}, CacheDependencies: {string.Join(", ", EntitySets)}.";
        }
    }
}
