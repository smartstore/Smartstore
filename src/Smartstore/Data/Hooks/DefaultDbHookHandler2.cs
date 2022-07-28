using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Collections;

namespace Smartstore.Data.Hooks
{
    public class DefaultDbHookHandler2 : IDbHookHandler
    {
        private readonly IDbHookRegistry _registry;
        private readonly IDbHookActivator _activator;

        // Prevents repetitive hooking of the same entity/state/[pre|post] combination within a single request
        private readonly HashSet<HookedEntityKey> _hookedEntities = new();

        public DefaultDbHookHandler2(IDbHookRegistry registry, IDbHookActivator activator)
        {
            _registry = registry;
            _activator = activator;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public async Task<DbSavingChangesResult> SavingChangesAsync(
            IEnumerable<IHookedEntity> entries,
            HookImportance minHookImportance = HookImportance.Normal,
            CancellationToken cancelToken = default)
        {
            Guard.NotNull(entries, nameof(entries));

            var anyStateChanged = false;

            if (!entries.Any())
            {
                return DbSavingChangesResult.Empty;
            }

            var processedHooks = new Multimap<IDbSaveHook, IHookedEntity>();

            foreach (var entry in entries)
            {
                var e = entry; // Prevents access to modified closure

                if (cancelToken.IsCancellationRequested)
                {
                    continue;
                }

                if (HandledAlready(e, HookStage.PreSave))
                {
                    // Prevent repetitive hooking of the same entity/state/pre combination within a single request
                    continue;
                }

                var hooks = _registry.SelectHooks(e, HookStage.PreSave, minHookImportance);

                foreach (var hook in hooks)
                {
                    // call hook
                    try
                    {
                        Logger.Debug("PRE save hook: {0}, State: {1}, Entity: {2}", hook.GetType().Name, e.InitialState, e.Entity.GetType().Name);

                        var instance = _activator.Activate(hook);
                        var result = await instance.OnBeforeSaveAsync(e, cancelToken);

                        if (result == HookResult.Ok)
                        {
                            processedHooks.Add(instance, e);
                        }
                        else if (result == HookResult.Void)
                        {
                            _registry.RegisterVoidHook(hook, e, HookStage.PreSave);
                        }
                    }
                    catch (Exception ex) when (ex is NotImplementedException || ex is NotSupportedException)
                    {
                        _registry.RegisterVoidHook(hook, e, HookStage.PreSave);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "PreSaveHook exception ({0})", hook.GetType().FullName);
                    }

                    // change state if applicable
                    if (e.HasStateChanged)
                    {
                        e.InitialState = e.State;
                        anyStateChanged = true;
                    }
                }
            }

            foreach (var hook in processedHooks)
            {
                await hook.Key.OnBeforeSaveCompletedAsync(hook.Value, cancelToken);
            }

            return new DbSavingChangesResult(processedHooks.Keys, anyStateChanged) { Entries = entries };
        }

        public async Task<DbSaveChangesResult> SavedChangesAsync(
            IEnumerable<IHookedEntity> entries,
            HookImportance minHookImportance = HookImportance.Normal,
            CancellationToken cancelToken = default)
        {
            Guard.NotNull(entries, nameof(entries));

            if (!entries.Any())
            {
                return DbSaveChangesResult.Empty;
            }    

            var processedHooks = new Multimap<IDbSaveHook, IHookedEntity>();

            foreach (var entry in entries)
            {
                var e = entry; // Prevents access to modified closure

                if (cancelToken.IsCancellationRequested)
                {
                    continue;
                }

                if (HandledAlready(e, HookStage.PostSave))
                {
                    // Prevent repetitive hooking of the same entity/state/post combination within a single request
                    continue;
                }

                var hooks = _registry.SelectHooks(e, HookStage.PostSave, minHookImportance);

                foreach (var hook in hooks)
                {
                    // Call hook
                    try
                    {
                        Logger.Debug("POST save hook: {0}, State: {1}, Entity: {2}", hook.GetType().Name, e.InitialState, e.Entity.GetType().Name);

                        var instance = _activator.Activate(hook);
                        var result = await instance.OnAfterSaveAsync(e, cancelToken);

                        if (result == HookResult.Ok)
                        {
                            processedHooks.Add(instance, e);
                        }
                        else if (result == HookResult.Void)
                        {
                            _registry.RegisterVoidHook(hook, e, HookStage.PostSave);
                        }
                    }
                    catch (Exception ex) when (ex is NotImplementedException || ex is NotSupportedException)
                    {
                        _registry.RegisterVoidHook(hook, e, HookStage.PostSave);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "PostSaveHook exception ({0})", hook.GetType().FullName);
                    }
                }
            }

            foreach (var hook in processedHooks)
            {
                await hook.Key.OnAfterSaveCompletedAsync(hook.Value, cancelToken);
            }

            return new DbSaveChangesResult(processedHooks.Keys);
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
