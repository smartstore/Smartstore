using System;
using System.Linq;
using System.Collections.Generic;

namespace Smartstore.Caching
{
    public sealed class CacheEntryOptions
    {
        private TimeSpan? _duration;
        private HashSet<string> _dependencies;
        
        public CacheEntryOptions ExpiresIn(TimeSpan duration)
        {
            _duration = duration;
            return this;
        }

        public CacheEntryOptions NoExpiration()
        {
            _duration = null;
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
                Duration = _duration
            };

            if (_dependencies != null)
            {
                entry.Dependencies = _dependencies.ToArray();
            }

            return entry;
        }
    }
}