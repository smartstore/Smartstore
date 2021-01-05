using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Collections;

namespace Smartstore.Data.Hooks
{
    public class DefaultDbHookHandler : IDbHookHandler
    {
        private readonly IEnumerable<Lazy<IDbSaveHook, HookMetadata>> _saveHooks;

        private readonly Multimap<RequestHookKey, IDbSaveHook> _hooksRequestCache = new Multimap<RequestHookKey, IDbSaveHook>();

        // Prevents repetitive hooking of the same entity/state/[pre|post] combination within a single request
        private readonly HashSet<HookedEntityKey> _hookedEntities = new HashSet<HookedEntityKey>();

        private static HashSet<Type> _importantSaveHookTypes;
        private readonly static object _lock = new object();

        // Contains all IDbSaveHook/EntityType/State/Stage combinations in which
        // the implementor threw either NotImplementedException or NotSupportedException.
        // This boosts performance because these VOID combinations are not processed again
        // and frees us mostly from the obligation always to detect changes.
        private readonly static HashSet<HookKey> _voidHooks = new HashSet<HookKey>();

        public DefaultDbHookHandler(IEnumerable<Lazy<IDbSaveHook, HookMetadata>> hooks)
        {
            _saveHooks = hooks;
        }

        public ILogger Logger
        {
            get;
            set;
        } = NullLogger.Instance;

        public bool HasImportantSaveHooks()
        {
            if (_importantSaveHookTypes == null)
            {
                lock (_lock)
                {
                    if (_importantSaveHookTypes == null)
                    {
                        _importantSaveHookTypes = new HashSet<Type>();
                        _importantSaveHookTypes.AddRange(_saveHooks.Where(x => x.Metadata.Important).Select(x => x.Metadata.ImplType));
                    }
                }
            }

            return _importantSaveHookTypes.Any();
        }

        public async Task<DbSavingChangesResult> SavingChangesAsync(IEnumerable<IHookedEntity> entries, bool importantHooksOnly, CancellationToken cancelToken = default)
        {
            Guard.NotNull(entries, nameof(entries));

            var anyStateChanged = false;

            if (!entries.Any() || !_saveHooks.Any() || (importantHooksOnly && !this.HasImportantSaveHooks()))
                return DbSavingChangesResult.Empty;

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

                var hooks = GetSaveHookInstancesFor(e, HookStage.PreSave, importantHooksOnly);

                foreach (var hook in hooks)
                {
                    // call hook
                    try
                    {
                        Logger.Debug("PRE save hook: {0}, State: {1}, Entity: {2}", hook.GetType().Name, e.InitialState, e.Entity.GetType().Name);
                        var result = await hook.OnBeforeSaveAsync(e, cancelToken);
                        
                        if (result == HookResult.Ok)
                        {
                            processedHooks.Add(hook, e);
                        }
                        else if (result == HookResult.Void)
                        {
                            RegisterVoidHook(hook, e, HookStage.PreSave);
                        }
                    }
                    catch (Exception ex) when (ex is NotImplementedException || ex is NotSupportedException)
                    {
                        RegisterVoidHook(hook, e, HookStage.PreSave);
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

        public async Task<DbSaveChangesResult> SavedChangesAsync(IEnumerable<IHookedEntity> entries, bool importantHooksOnly, CancellationToken cancelToken = default)
        {
            Guard.NotNull(entries, nameof(entries));

            if (!entries.Any() || !_saveHooks.Any() || (importantHooksOnly && !this.HasImportantSaveHooks()))
                return DbSaveChangesResult.Empty;

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

                var hooks = GetSaveHookInstancesFor(e, HookStage.PostSave, importantHooksOnly);

                foreach (var hook in hooks)
                {
                    // call hook
                    try
                    {
                        Logger.Debug("POST save hook: {0}, State: {1}, Entity: {2}", hook.GetType().Name, e.InitialState, e.Entity.GetType().Name);
                        var result = await hook.OnAfterSaveAsync(e, cancelToken);

                        if (result == HookResult.Ok)
                        {
                            processedHooks.Add(hook, e);
                        }
                        else if (result == HookResult.Void)
                        {
                            RegisterVoidHook(hook, e, HookStage.PostSave);
                        }
                    }
                    catch (Exception ex) when (ex is NotImplementedException || ex is NotSupportedException)
                    {
                        RegisterVoidHook(hook, e, HookStage.PostSave);
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

        private IEnumerable<IDbSaveHook> GetSaveHookInstancesFor(IHookedEntity entry, HookStage stage, bool importantOnly)
        {
            if (entry.EntityType == null)
            {
                return Enumerable.Empty<IDbSaveHook>();
            }

            IEnumerable<IDbSaveHook> hooks;

            // For request cache lookup
            var requestKey = new RequestHookKey(entry, stage, importantOnly);

            if (_hooksRequestCache.ContainsKey(requestKey))
            {
                hooks = _hooksRequestCache[requestKey];
            }
            else
            {
                hooks = _saveHooks
                    // Reduce by data context types
                    .Where(x => x.Metadata.DbContextType.IsAssignableFrom(entry.DbContext.GetType()))
                    // Reduce by entity types which can be processed by this hook
                    .Where(x => x.Metadata.HookedType.IsAssignableFrom(entry.EntityType))
                    // When importantOnly, only include hook types with [ImportantAttribute]
                    .Where(x => !importantOnly || _importantSaveHookTypes.Contains(x.Metadata.ImplType))
                    // Exclude void hooks (hooks known to be useless for the current EntityType/State/Stage combination)
                    .Where(x => !_voidHooks.Contains(new HookKey(x.Metadata.ImplType, entry, stage)))
                    // Apply sort
                    .OrderBy(x => x.Metadata.Order)
                    // Get the hook instance
                    .Select(x => x.Value)
                    // Make array
                    .ToArray();

                _hooksRequestCache.AddRange(requestKey, hooks);
            }

            return hooks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HandledAlready(IHookedEntity entry, HookStage stage)
        {
            var entity = entry.Entity;

            if (entity == null || entity.IsTransientRecord())
                return false;

            var key = new HookedEntityKey(entry, stage, entity.Id);
            if (_hookedEntities.Contains(key))
            {
                return true;
            }

            _hookedEntities.Add(key);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterVoidHook(IDbSaveHook hook, IHookedEntity entry, HookStage stage)
        {
            var hookType = hook.GetType();

            // Unregister from request cache (if cached)
            _hooksRequestCache.Remove(new RequestHookKey(entry, stage, false), hook);
            _hooksRequestCache.Remove(new RequestHookKey(entry, stage, true), hook);

            lock (_lock)
            {
                // Add to static void hooks set
                _voidHooks.Add(new HookKey(hookType, entry, stage));
            }
        }

        enum HookStage
        {
            PreSave,
            PostSave
        }

        class HookedEntityKey : Tuple<Type, Type, int, EntityState, HookStage>
        {
            public HookedEntityKey(IHookedEntity entry, HookStage stage, int entityId)
                : base(entry.DbContext.GetType(), entry.EntityType, entityId, entry.InitialState, stage)
            {
            }
        }

        class RequestHookKey : Tuple<Type, Type, EntityState, HookStage, bool>
        {
            public RequestHookKey(IHookedEntity entry, HookStage stage, bool importantOnly)
                : base(entry.DbContext.GetType(), entry.EntityType, entry.InitialState, stage, importantOnly)
            {
            }
        }

        class HookKey : Tuple<Type, Type, EntityState, HookStage>
        {
            public HookKey(Type hookType, IHookedEntity entry, HookStage stage)
                : base(hookType, entry.EntityType, entry.InitialState, stage)
            {
            }
        }
    }
}