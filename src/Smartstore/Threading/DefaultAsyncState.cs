using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Smartstore.Caching;

namespace Smartstore.Threading
{
    public partial class DefaultAsyncState : IAsyncState
    {
        const string KeyPrefix = "asyncstate:";

        public DefaultAsyncState(ICacheStore cache)
        {
            Store = cache;
        }

        public ICacheStore Store { get; }

        public virtual bool Contains<T>(string name = null)
        {
            return Store.Contains(BuildKey<T>(name));
        }

        public virtual T Get<T>(string name = null)
        {
            var entry = GetStateInfo<T>(name);

            if (entry != null)
            {
                return (T)entry.Value;
            }

            return default;
        }

        public virtual IEnumerable<T> GetAll<T>()
        {
            var keyPrefix = BuildKey<T>(null);
            return Store
                .Keys(keyPrefix + "*")
                .Select(key => Store.Get(key))
                .Where(entry => entry?.Value != null)
                .Select(entry => entry.Value)
                .OfType<T>();
        }

        public virtual void Create<T>(T state, string name = null, bool neverExpires = false, CancellationTokenSource cancelTokenSource = default)
        {
            Guard.NotNull(state, nameof(state));

            var key = BuildKey<T>(name);
            var entry = new CacheEntry
            {
                Key = key,
                Value = state,
                ValueType = typeof(T),
                Priority = CacheEntryPriority.NeverRemove,
                Duration = neverExpires ? null : TimeSpan.FromMinutes(15),
                CancellationTokenSource = cancelTokenSource,
                CancelTokenSourceOnRemove = false
            };

            Store.Put(key, entry);
        }

        public virtual bool Update<T>(Action<T> update, string name = null)
        {
            Guard.NotNull(update, nameof(update));

            var entry = GetStateInfo<T>(name);

            if (entry != null)
            {
                var state = (T)entry.Value;
                if (state != null)
                {
                    update(state);

                    if (Store.IsDistributed)
                    {
                        Store.Put(entry.Key, entry);
                    }

                    return true;
                }
            }

            return false;
        }

        public virtual void Remove<T>(string name = null)
        {
            Store.Remove(BuildKey<T>(name));
        }

        public virtual bool Cancel<T>(string name = null)
        {
            var entry = Store.Get(BuildKey<T>(name));

            if (entry != null)
            {
                if (!entry.CancellationTokenSource.IsCancellationRequested)
                {
                    entry.CancellationTokenSource.Cancel();
                }
                
                return true;
            }

            return false;
        }

        protected virtual CacheEntry GetStateInfo<T>(string name = null)
        {
            var key = BuildKey<T>(name);
            
            if (Store.GetTimeToLive(key) != null)
            {
                // Mimic sliding expiration behavior. Extend expiry by 15 min.,
                // but only if an expiration was set before.
                Store.SetTimeToLive(key, TimeSpan.FromMinutes(15));
            }

            return Store.Get(key);
        }

        protected string BuildKey<T>(string name)
        {
            return BuildKey(typeof(T), name);
        }

        protected virtual string BuildKey(Type type, string name)
        {
            return KeyPrefix + type.FullName + name.LeftPad(pad: ':');
        }
    }
}