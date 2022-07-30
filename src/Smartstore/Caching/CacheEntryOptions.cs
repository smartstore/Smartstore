namespace Smartstore.Caching
{
    public sealed class CacheEntryOptions
    {
        private TimeSpan? _absoluteExpiration;
        private TimeSpan? _slidingExpiration;
        private HashSet<string> _dependencies;

        public CacheEntryOptions ExpiresIn(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "The expiration value must be positive.");
            }

            _absoluteExpiration = duration;
            return this;
        }

        public CacheEntryOptions SetSlidingExpiration(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "The expiration value must be positive.");
            }

            _slidingExpiration = duration;
            return this;
        }

        public CacheEntryOptions NoExpiration()
        {
            _absoluteExpiration = null;
            _slidingExpiration = null;
            return this;
        }

        public CacheEntryOptions DependsOn(params string[] keys)
        {
            if (keys.Length > 0)
            {
                if (_dependencies == null)
                {
                    _dependencies = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    _dependencies.AddRange(keys);
                }
            }

            return this;
        }

        public CacheEntry AsEntry(string key, object value)
        {
            var entry = new CacheEntry
            {
                Key = key,
                Value = value,
                ValueType = value?.GetType(),
                AbsoluteExpiration = _absoluteExpiration,
                SlidingExpiration = _slidingExpiration
            };

            if (_dependencies != null)
            {
                entry.Dependencies = _dependencies.ToArray();
            }

            return entry;
        }
    }
}