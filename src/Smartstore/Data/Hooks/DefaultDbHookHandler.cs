using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Collections;

namespace Smartstore.Data.Hooks
{
    public class DefaultDbHookHandler : IDbHookHandler
    {
        const string PreSaveHook = "PreSaveHook";
        const string PostSaveHook = "PostSaveHook";

        private readonly IDbHookRegistry _registry;
        private readonly IDbHookActivator _activator;
        private readonly bool _hasHooks;

        // Prevents repetitive hooking of the same entity/state/[pre|post] combination within a single request
        private readonly HashSet<HookedEntityKey> _hookedEntities = new();

        public DefaultDbHookHandler(IDbHookRegistry registry, IDbHookActivator activator)
        {
            _registry = registry;
            _activator = activator;
            _hasHooks = _registry.GetAllMetadata().Length > 0;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<DbSavingChangesResult> SavingChangesAsync(
            IHookedEntity[] entries,
            HookImportance minHookImportance = HookImportance.Normal,
            CancellationToken cancelToken = default)
        {
            return (DbSavingChangesResult)(await SaveChangesInternal(HookStage.PreSave, entries, minHookImportance, cancelToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<DbSaveChangesResult> SavedChangesAsync(
            IHookedEntity[] entries,
            HookImportance minHookImportance = HookImportance.Normal,
            CancellationToken cancelToken = default)
        {
            return SaveChangesInternal(HookStage.PostSave, entries, minHookImportance, cancelToken);
        }

        public async Task<DbSaveChangesResult> SaveChangesInternal(
            HookStage stage,
            IHookedEntity[] entries,
            HookImportance minHookImportance,
            CancellationToken cancelToken)
        {
            Guard.NotNull(entries, nameof(entries));

            var pre = stage == HookStage.PreSave;
            if (!_hasHooks || entries.Length == 0)
            {
                return pre ? DbSavingChangesResult.Empty : DbSaveChangesResult.Empty;
            }

            var messagePrefix = pre ? PreSaveHook : PostSaveHook;
            var anyStateChanged = false;

            var processedHooks = new Multimap<IDbSaveHook, IHookedEntity>();

            // Determine whether ALL entities in the batch share the same type and state.
            // If so, work with the hooks for the first entity, don't bother repetitive
            // resolution of the very same hooks.
            TryGetSharedHooks(entries, stage, minHookImportance, out var sharedHooks);

            for (var i = 0; i < entries.Length; i++)
            {
                var e = entries[i];

                if (cancelToken.IsCancellationRequested)
                {
                    continue;
                }

                if (HandledAlready(e, stage))
                {
                    // Prevent repetitive hooking of the same entity/state/pre combination within a single request
                    continue;
                }

                var hooks = sharedHooks ?? _registry.SelectHooks(e, stage, minHookImportance);

                for (var y = 0; y < hooks.Length; y++)
                {
                    var hook = hooks[y];

                    // call hook
                    try
                    {
                        Logger.Debug($"{messagePrefix}: {hook.GetType().Name}, State: {e.InitialState}, Entity: {e.Entity.GetType().Name}");

                        var instance = _activator.Activate(hook);
                        var result = pre
                            ? await instance.OnBeforeSaveAsync(e, cancelToken)
                            : await instance.OnAfterSaveAsync(e, cancelToken);

                        if (result == HookResult.Ok)
                        {
                            processedHooks.Add(instance, e);
                        }
                        else if (result == HookResult.Void)
                        {
                            _registry.RegisterVoidHook(hook, e, stage);
                        }
                    }
                    catch (Exception ex) when (ex is NotImplementedException || ex is NotSupportedException)
                    {
                        _registry.RegisterVoidHook(hook, e, stage);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"{messagePrefix} exception ({hook.GetType().FullName})");
                    }

                    // Change state if applicable
                    if (pre && e.HasStateChanged)
                    {
                        e.InitialState = e.State;
                        anyStateChanged = true;
                    }
                }
            }

            foreach (var hook in processedHooks)
            {
                if (pre)
                {
                    await hook.Key.OnBeforeSaveCompletedAsync(hook.Value, cancelToken);
                }
                else
                {
                    await hook.Key.OnAfterSaveCompletedAsync(hook.Value, cancelToken);
                }
            }

            return pre
                ? new DbSavingChangesResult(processedHooks.Keys, anyStateChanged) { Entries = entries }
                : new DbSaveChangesResult(processedHooks.Keys);
        }

        /// <summary>
        /// Tries to resolve shared hooks if all entity types and states 
        /// in the <paramref name="entries"/> batch are all identical.
        /// </summary>
        private bool TryGetSharedHooks(
            IHookedEntity[] entries,
            HookStage stage,
            HookImportance minHookImportance,
            out HookMetadata[] sharedHooks)
        {
            sharedHooks = null;

            if (entries.Length > 3)
            {
                var entry = entries[0];

                for (var i = 1; i < entries.Length; i++)
                {
                    if (entries[i].InitialState != entry.InitialState || entries[i].EntityType != entry.EntityType)
                    {
                        return false;
                    }
                }

                sharedHooks = _registry.SelectHooks(entry, stage, minHookImportance);
            }

            return sharedHooks != null;
        }

        private bool HandledAlready(IHookedEntity entry, HookStage stage)
        {
            var entity = entry.Entity;

            if (entity == null || entity.IsTransientRecord())
            {
                return false;
            }

            var key = new HookedEntityKey(entry, stage, entity.Id);
            if (_hookedEntities.Contains(key))
            {
                return true;
            }

            _hookedEntities.Add(key);
            return false;
        }

        class HookedEntityKey : Tuple<Type, Type, int, EntityState, HookStage>
        {
            public HookedEntityKey(IHookedEntity entry, HookStage stage, int entityId)
                : base(entry.DbContext.GetType(), entry.EntityType, entityId, entry.InitialState, stage)
            {
            }
        }
    }
}
