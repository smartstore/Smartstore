namespace Smartstore.Data.Caching
{
    public class DbCachingPolicy
    {
        public DbCachingPolicy()
        {
        }

        public DbCachingPolicy(CacheableEntityAttribute attribute)
        {
            NoCaching = attribute.NeverCache;
            Merge(attribute);
        }

        internal DbCachingPolicy Merge(CacheableEntityAttribute attribute)
        {
            // Merge global policy with query policy
            if (ExpirationTimeout == null && attribute?.Expiry > 0)
            {
                ExpirationTimeout = TimeSpan.FromMinutes(attribute.Expiry);
            }

            if (MaxRows == null && attribute?.MaxRows > 0)
            {
                MaxRows = attribute.MaxRows;
            }

            return this;
        }

        internal bool NoCaching { get; set; }

        /// <summary>
        /// Gets or sets a max rows limit. Query results with more items than the given number will not be cached.
        /// </summary>
        public int? MaxRows { get; internal set; }

        /// <summary>
        /// Gets or sets the expiration timeout. Default value 3 hours.
        /// </summary>
        public TimeSpan? ExpirationTimeout { get; internal set; }

        public override string ToString()
        {
            return $"Timeout: {ExpirationTimeout}, MaxRows: {MaxRows}";
        }
    }
}