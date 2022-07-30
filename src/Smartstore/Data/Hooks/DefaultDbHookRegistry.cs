using Smartstore.Threading;

namespace Smartstore.Data.Hooks
{
    public class DefaultDbHookRegistry : IDbHookRegistry
    {
        private readonly ReaderWriterLockSlim _rwLock = new();
        private readonly HookMetadata[] _allMetadata;
        private readonly Dictionary<HookKey, HookMetadata[]> _hookCache = new();

        public DefaultDbHookRegistry(IEnumerable<Lazy<IDbSaveHook, HookMetadata>> hooks)
        {
            _allMetadata = hooks.Select(x => x.Metadata).OrderBy(x => x.Order).ToArray();
        }

        public HookMetadata[] GetAllMetadata()
            => _allMetadata;

        public virtual HookMetadata[] SelectHooks(
            IHookedEntity entry,
            HookStage stage,
            HookImportance minHookImportance = HookImportance.Normal)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            HookMetadata[] hooks;
            var cacheKey = new HookKey(entry, stage);

            using (_rwLock.GetUpgradeableReadLock())
            {
                if (!_hookCache.TryGetValue(cacheKey, out hooks))
                {
                    using (_rwLock.GetWriteLock())
                    {
                        // Double check
                        if (!_hookCache.TryGetValue(cacheKey, out hooks))
                        {
                            _hookCache[cacheKey] = hooks = SelectHooks(_allMetadata, entry.EntityType).ToArray();
                        }
                    }
                }
            }

            if (hooks.Length > 0 && minHookImportance > HookImportance.Normal)
            {
                // Only select hook types with Importance >= minHookImportance
                return hooks.Where(x => x.Importance >= minHookImportance).ToArray();
            }

            return hooks;
        }

        public virtual void RegisterVoidHook(HookMetadata voidHook, IHookedEntity entry, HookStage stage)
        {
            if (voidHook == null)
            {
                throw new ArgumentNullException(nameof(voidHook));
            }

            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            using (_rwLock.GetWriteLock())
            {
                var cacheKey = new HookKey(entry, stage);

                if (_hookCache.TryGetValue(cacheKey, out var hooks))
                {
                    // Remove void hook from previously built hook array
                    _hookCache[cacheKey] = hooks
                        .Where(x => x != voidHook)
                        .ToArray();
                }
            }
        }

        private static IEnumerable<HookMetadata> SelectHooks(HookMetadata[] source, Type entityType)
        {
            return source
                // Reduce by entity types which can be processed by this hook
                .Where(x => x.HookedType.IsAssignableFrom(entityType));
        }

        class HookKey : Tuple<Type, EntityState, HookStage>
        {
            public HookKey(IHookedEntity entry, HookStage stage)
                : base(entry.EntityType, entry.InitialState, stage)
            {
            }

            public HookKey(Type entityType, EntityState state, HookStage stage)
                : base(entityType, state, stage)
            {
            }
        }
    }
}
